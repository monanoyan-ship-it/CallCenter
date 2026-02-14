/**
 * SIP.js Wrapper - Blazor JS Interop icin
 * SIP.js UserAgent API kullanarak WebRTC uzerinden SIP baglantisi kurar.
 *
 * Gereklilikler:
 * - SIP.js 0.21.2 (CDN ile yuklenmis olmali)
 * - HTTPS (getUserMedia icin zorunlu)
 * - SIP saglayicinin WSS (WebSocket Secure) destegi
 */

// Global SIP client state
window.sipClient = {
    userAgent: null,
    registerer: null,
    currentSession: null,
    dotNetRef: null,
    isRegistered: false,
    isOnHold: false,
    remoteAudio: null,
    remoteVideo: null,          // Remote video element
    localVideo: null,           // Local video preview element
    localVideoStream: null,     // Local video MediaStream
    videoEnabled: false,        // Video aktif mi
    preferredCodecs: null,      // Codec oncelik sirasi: ["opus","g722","pcmu","pcma"]
    jitterBufferTarget: 0,      // Jitter buffer hedef gecikme (ms). 0 = varsayilan.
    _networkListenerActive: false,

    /**
     * SIP client'i baslatir ve register olur.
     * @param {string} wsUri - WebSocket URI (ornek: wss://sip.example.com:8443)
     * @param {string} sipUri - SIP URI (ornek: sip:1001@sip.example.com)
     * @param {string} authUser - SIP kullanici adi
     * @param {string} authPass - SIP sifresi
     * @param {string} displayName - Arayan adi
     * @param {object} dotNetRef - Blazor DotNetObjectReference (C# callback icin)
     * @param {string|null} stunServer - STUN sunucu (ornek: stun:stun.l.google.com:19302)
     * @param {string|null} turnServer - TURN sunucu (ornek: turn:turn.example.com:3478)
     * @param {string|null} turnUsername - TURN kullanici adi
     * @param {string|null} turnPassword - TURN sifresi
     */
    initialize: async function (wsUri, sipUri, authUser, authPass, displayName, dotNetRef,
        stunServer, turnServer, turnUsername, turnPassword,
        preferredCodecsJson, jitterBufferMinMs, jitterBufferMaxMs) {
        try {
            this.dotNetRef = dotNetRef;
            this.remoteAudio = document.getElementById('remoteAudio');

            // Mevcut baglanti varsa temizle
            if (this.userAgent) {
                await this.dispose();
            }

            const uri = SIP.UserAgent.makeURI(sipUri);
            if (!uri) {
                console.error('[SipClient] Gecersiz SIP URI:', sipUri);
                await this._notifyDotNet('OnRegistrationFailed', 'Gecersiz SIP URI: ' + sipUri);
                return false;
            }

            // ICE sunuculari olustur (STUN + TURN)
            const iceServers = this._buildIceServers(stunServer, turnServer, turnUsername, turnPassword);
            console.log('[SipClient] ICE sunuculari:', iceServers.length > 0 ? iceServers : 'varsayilan');

            // Codec tercihleri
            if (preferredCodecsJson) {
                try {
                    this.preferredCodecs = JSON.parse(preferredCodecsJson);
                    console.log('[SipClient] Codec tercihleri:', this.preferredCodecs);
                } catch (e) {
                    console.warn('[SipClient] Codec JSON parse hatasi:', e);
                    this.preferredCodecs = null;
                }
            }

            // Jitter buffer ayari
            this.jitterBufferTarget = jitterBufferMaxMs || 0;

            this.userAgent = new SIP.UserAgent({
                uri: uri,
                authorizationUsername: authUser,
                authorizationPassword: authPass,
                displayName: displayName || authUser,
                transportOptions: {
                    server: wsUri
                },
                sessionDescriptionHandlerFactoryOptions: {
                    constraints: {
                        audio: {
                            echoCancellation: true,
                            noiseSuppression: true,
                            autoGainControl: true
                        },
                        video: false
                    },
                    peerConnectionConfiguration: iceServers.length > 0
                        ? { iceServers: iceServers }
                        : undefined
                },
                logLevel: 'warn'
            });

            // Gelen arama dinleyicisi
            this.userAgent.delegate = {
                onInvite: (invitation) => {
                    this._handleIncomingCall(invitation);
                }
            };

            // Transport event'leri
            this.userAgent.transport.onConnect = () => {
                console.log('[SipClient] WebSocket baglandi');
            };

            this.userAgent.transport.onDisconnect = (error) => {
                console.log('[SipClient] WebSocket koptu', error);
                this.isRegistered = false;
                this._notifyDotNet('OnRegistrationFailed', 'WebSocket baglantisi koptu');
            };

            // UserAgent'i baslat
            await this.userAgent.start();

            // Register ol
            this.registerer = new SIP.Registerer(this.userAgent);

            this.registerer.stateChange.addListener((state) => {
                switch (state) {
                    case SIP.RegistererState.Registered:
                        console.log('[SipClient] Kayit basarili');
                        this.isRegistered = true;
                        this._notifyDotNet('OnRegistered', '');
                        break;
                    case SIP.RegistererState.Unregistered:
                        console.log('[SipClient] Kayit silindi');
                        this.isRegistered = false;
                        break;
                    case SIP.RegistererState.Terminated:
                        console.log('[SipClient] Registerer sonlandi');
                        this.isRegistered = false;
                        break;
                }
            });

            await this.registerer.register();
            return true;

        } catch (err) {
            console.error('[SipClient] Baslama hatasi:', err);
            await this._notifyDotNet('OnRegistrationFailed', err.message || 'Bilinmeyen hata');
            return false;
        }
    },

    /**
     * Dis arama yapar (outbound INVITE).
     * @param {string} destination - Aranacak numara veya SIP URI
     */
    makeCall: async function (destination) {
        if (!this.userAgent || !this.isRegistered) {
            console.error('[SipClient] SIP kayitli degil, arama yapilamaz');
            return false;
        }

        if (this.currentSession) {
            console.warn('[SipClient] Zaten aktif bir arama var');
            return false;
        }

        try {
            // Hedef URI olustur
            let targetUri;
            if (destination.startsWith('sip:')) {
                targetUri = SIP.UserAgent.makeURI(destination);
            } else {
                // Sadece numara verilmisse, domain'i SIP URI'den al
                const domain = this.userAgent.configuration.uri.host;
                targetUri = SIP.UserAgent.makeURI('sip:' + destination + '@' + domain);
            }

            if (!targetUri) {
                console.error('[SipClient] Gecersiz hedef:', destination);
                return false;
            }

            const inviter = new SIP.Inviter(this.userAgent, targetUri, {
                sessionDescriptionHandlerOptions: {
                    constraints: {
                        audio: {
                            echoCancellation: true,
                            noiseSuppression: true,
                            autoGainControl: true
                        },
                        video: false
                    }
                }
            });

            this.currentSession = inviter;
            this.isOnHold = false;
            this._setupSessionEvents(inviter);

            await inviter.invite();
            console.log('[SipClient] Arama baslatildi:', destination);
            return true;

        } catch (err) {
            console.error('[SipClient] Arama hatasi:', err);
            this.currentSession = null;
            await this._notifyDotNet('OnCallFailed', err.message || 'Arama baslatilamadi');
            return false;
        }
    },

    /**
     * Gelen aramayi kabul eder.
     */
    answerCall: async function () {
        if (!this.currentSession || !(this.currentSession instanceof SIP.Invitation)) {
            console.warn('[SipClient] Cevaplanacak gelen arama yok');
            return false;
        }

        try {
            await this.currentSession.accept({
                sessionDescriptionHandlerOptions: {
                    constraints: {
                        audio: {
                            echoCancellation: true,
                            noiseSuppression: true,
                            autoGainControl: true
                        },
                        video: false
                    }
                }
            });
            console.log('[SipClient] Arama kabul edildi');
            return true;
        } catch (err) {
            console.error('[SipClient] Cevaplama hatasi:', err);
            return false;
        }
    },

    /**
     * Aktif aramayi kapatir (BYE) veya gelen aramayi reddeder.
     */
    hangup: async function () {
        if (!this.currentSession) {
            console.warn('[SipClient] Kapatilacak arama yok');
            return false;
        }

        try {
            const session = this.currentSession;
            const state = session.state;

            switch (state) {
                case SIP.SessionState.Initial:
                case SIP.SessionState.Establishing:
                    // Henuz kurulmamis — cancel veya reject
                    if (session instanceof SIP.Inviter) {
                        await session.cancel();
                    } else if (session instanceof SIP.Invitation) {
                        await session.reject();
                    }
                    break;
                case SIP.SessionState.Established:
                    // Aktif gorusme — BYE gonder
                    await session.bye();
                    break;
                default:
                    console.warn('[SipClient] Session durumu:', state);
                    break;
            }

            this.currentSession = null;
            this.isOnHold = false;
            console.log('[SipClient] Arama kapatildi');
            return true;

        } catch (err) {
            console.error('[SipClient] Kapatma hatasi:', err);
            this.currentSession = null;
            this.isOnHold = false;
            return false;
        }
    },

    /**
     * Aramayi bekletir (HOLD).
     * SIP.js 0.21.x'te SIP.Web.holdModifier mevcut degil,
     * bu yuzden SDP'yi dogrudan manipule ediyoruz.
     */
    holdCall: async function () {
        if (!this.currentSession || this.currentSession.state !== SIP.SessionState.Established) {
            return false;
        }

        try {
            const sdh = this.currentSession.sessionDescriptionHandler;
            if (sdh && sdh.peerConnection) {
                sdh.peerConnection.getSenders().forEach(sender => {
                    if (sender.track) sender.track.enabled = false;
                });
            }
            // re-INVITE ile hold (SDP sendonly) — custom modifier
            await this.currentSession.invite({
                sessionDescriptionHandlerModifiers: [this._holdModifier.bind(this)]
            });
            this.isOnHold = true;
            console.log('[SipClient] Arama beklemede');
            return true;
        } catch (err) {
            console.error('[SipClient] Hold hatasi:', err);
            return false;
        }
    },

    /**
     * Beklemedeki aramayi devam ettirir (UNHOLD).
     */
    unholdCall: async function () {
        if (!this.currentSession || this.currentSession.state !== SIP.SessionState.Established) {
            return false;
        }

        try {
            const sdh = this.currentSession.sessionDescriptionHandler;
            if (sdh && sdh.peerConnection) {
                sdh.peerConnection.getSenders().forEach(sender => {
                    if (sender.track) sender.track.enabled = true;
                });
            }
            await this.currentSession.invite();
            this.isOnHold = false;
            console.log('[SipClient] Arama devam ediyor');
            return true;
        } catch (err) {
            console.error('[SipClient] Unhold hatasi:', err);
            return false;
        }
    },

    /**
     * DTMF tonu gonderir.
     * Oncelik: RFC 2833 (RTP telephone-event), basarisizsa SIP INFO fallback.
     * @param {string} tone - DTMF karakteri (0-9, *, #)
     */
    sendDtmf: function (tone) {
        if (!this.currentSession || this.currentSession.state !== SIP.SessionState.Established) {
            return false;
        }

        try {
            // Yontem 1: RFC 2833 (in-band, RTP telephone-event) — tercih edilen
            const sdh = this.currentSession.sessionDescriptionHandler;
            if (sdh && typeof sdh.sendDtmf === 'function') {
                const sent = sdh.sendDtmf(tone);
                if (sent) {
                    console.log('[SipClient] DTMF (RFC 2833) gonderildi:', tone);
                    return true;
                }
            }

            // Yontem 2: SIP INFO fallback
            this.currentSession.info({
                requestOptions: {
                    body: {
                        contentDisposition: 'render',
                        contentType: 'application/dtmf-relay',
                        content: 'Signal=' + tone + '\r\nDuration=160'
                    }
                }
            });
            console.log('[SipClient] DTMF (SIP INFO) gonderildi:', tone);
            return true;
        } catch (err) {
            console.error('[SipClient] DTMF hatasi:', err);
            return false;
        }
    },

    /**
     * Blind transfer (REFER).
     * @param {string} target - Transfer hedefi (numara veya SIP URI)
     */
    transferCall: async function (target) {
        if (!this.currentSession || this.currentSession.state !== SIP.SessionState.Established) {
            return false;
        }

        try {
            let targetUri;
            if (target.startsWith('sip:')) {
                targetUri = SIP.UserAgent.makeURI(target);
            } else {
                const domain = this.userAgent.configuration.uri.host;
                targetUri = SIP.UserAgent.makeURI('sip:' + target + '@' + domain);
            }

            if (!targetUri) {
                console.error('[SipClient] Gecersiz transfer hedefi:', target);
                return false;
            }

            await this.currentSession.refer(targetUri);
            console.log('[SipClient] Transfer baslatildi:', target);
            return true;
        } catch (err) {
            console.error('[SipClient] Transfer hatasi:', err);
            return false;
        }
    },

    /**
     * Mevcut ses cihazlarini listeler.
     * @returns {Array} Cihaz listesi [{deviceId, label, kind}]
     */
    getAudioDevices: async function () {
        try {
            // Mikrofon izni al (cihaz etiketleri icin gerekli)
            await navigator.mediaDevices.getUserMedia({ audio: true });
            const devices = await navigator.mediaDevices.enumerateDevices();
            return devices
                .filter(d => d.kind === 'audioinput' || d.kind === 'audiooutput')
                .map(d => ({
                    deviceId: d.deviceId,
                    label: d.label || (d.kind === 'audioinput' ? 'Mikrofon' : 'Hoparlor'),
                    kind: d.kind
                }));
        } catch (err) {
            console.error('[SipClient] Cihaz listesi hatasi:', err);
            return [];
        }
    },

    /**
     * Ses cikis cihazini degistirir.
     * @param {string} deviceId - Cihaz ID'si
     */
    setAudioDevice: async function (deviceId) {
        try {
            if (this.remoteAudio && typeof this.remoteAudio.setSinkId === 'function') {
                await this.remoteAudio.setSinkId(deviceId);
                console.log('[SipClient] Ses cikis cihazi degistirildi:', deviceId);
                return true;
            }
            console.warn('[SipClient] setSinkId desteklenmiyor');
            return false;
        } catch (err) {
            console.error('[SipClient] Cihaz degistirme hatasi:', err);
            return false;
        }
    },

    /**
     * SIP kayit durumunu dondurur.
     */
    getRegistrationState: function () {
        return this.isRegistered;
    },

    /**
     * Aktif arama var mi?
     */
    hasActiveCall: function () {
        return this.currentSession != null &&
            (this.currentSession.state === SIP.SessionState.Established ||
                this.currentSession.state === SIP.SessionState.Establishing);
    },

    /**
     * Tum kaynaklari temizler ve baglantilari kapatir.
     */
    dispose: async function () {
        try {
            if (this.currentSession) {
                await this.hangup();
            }

            if (this.registerer) {
                try {
                    await this.registerer.unregister();
                } catch { }
                this.registerer = null;
            }

            if (this.userAgent) {
                try {
                    await this.userAgent.stop();
                } catch { }
                this.userAgent = null;
            }

            this.isRegistered = false;
            this.isOnHold = false;
            this.dotNetRef = null;
            console.log('[SipClient] Dispose tamamlandi');

        } catch (err) {
            console.error('[SipClient] Dispose hatasi:', err);
        }
    },

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE HELPER METODLAR
    // ═══════════════════════════════════════════════════════════════

    /**
     * ICE sunucu dizisi olusturur (STUN + TURN).
     * Bos parametreler atlanir.
     * @returns {Array} RTCIceServer dizisi
     */
    _buildIceServers: function (stunServer, turnServer, turnUsername, turnPassword) {
        const servers = [];

        if (stunServer) {
            servers.push({ urls: stunServer });
        }

        if (turnServer) {
            const turnConfig = { urls: turnServer };
            if (turnUsername) turnConfig.username = turnUsername;
            if (turnPassword) turnConfig.credential = turnPassword;
            servers.push(turnConfig);
        }

        return servers;
    },

    /**
     * Hold modifier — SDP'deki a=sendrecv satirlarini a=sendonly yapar.
     * SIP.js 0.21.x'te SIP.Web.holdModifier mevcut olmadigi icin
     * bu custom implementasyon kullanilir.
     * @param {RTCSessionDescriptionInit} description - SDP description
     * @returns {Promise<RTCSessionDescriptionInit>} Modified SDP
     */
    _holdModifier: function (description) {
        if (description.sdp) {
            description.sdp = description.sdp.replace(
                /a=sendrecv/g,
                'a=sendonly'
            );
        }
        return Promise.resolve(description);
    },

    /**
     * Gelen arama (INVITE) handler.
     */
    _handleIncomingCall: function (invitation) {
        console.log('[SipClient] Gelen arama:', invitation.remoteIdentity.uri.toString());

        // Zaten aktif arama varsa, yeni aramayi reddet
        if (this.currentSession) {
            console.warn('[SipClient] Aktif arama var, gelen arama reddedildi');
            invitation.reject();
            return;
        }

        this.currentSession = invitation;
        this.isOnHold = false;
        this._setupSessionEvents(invitation);

        // C#'a bildir
        const callerUri = invitation.remoteIdentity.uri.toString();
        const callerDisplay = invitation.remoteIdentity.displayName || '';
        this._notifyDotNet('OnIncomingCall', callerUri + '|' + callerDisplay);
    },

    /**
     * Session event'lerini kurar (hem Inviter hem Invitation icin).
     */
    _setupSessionEvents: function (session) {
        session.stateChange.addListener((state) => {
            console.log('[SipClient] Session durumu:', state);

            switch (state) {
                case SIP.SessionState.Establishing:
                    // Arama kuruluyor (caliyor)
                    break;

                case SIP.SessionState.Established:
                    // Arama kabul edildi — ses akisini baslat
                    this._setupRemoteMedia(session);
                    this._notifyDotNet('OnCallAnswered', '');
                    break;

                case SIP.SessionState.Terminated:
                    // Arama sonlandi
                    this.currentSession = null;
                    this.isOnHold = false;
                    this._notifyDotNet('OnCallEnded', '');
                    break;
            }
        });
    },

    /**
     * Uzak tarafin ses ve video akislarini ilgili elementlere baglar.
     */
    _setupRemoteMedia: function (session) {
        const sdh = session.sessionDescriptionHandler;
        if (!sdh || !sdh.peerConnection) return;

        const pc = sdh.peerConnection;
        const remoteAudioStream = new MediaStream();
        const remoteVideoStream = new MediaStream();

        pc.getReceivers().forEach(receiver => {
            if (receiver.track) {
                if (receiver.track.kind === 'audio') {
                    remoteAudioStream.addTrack(receiver.track);

                    // Jitter buffer ayari (destekleniyorsa)
                    if (this.jitterBufferTarget > 0 && receiver.jitterBufferTarget !== undefined) {
                        receiver.jitterBufferTarget = this.jitterBufferTarget;
                        console.log('[SipClient] Jitter buffer hedefi:', this.jitterBufferTarget, 'ms');
                    }
                } else if (receiver.track.kind === 'video') {
                    remoteVideoStream.addTrack(receiver.track);
                    console.log('[SipClient] Remote video track alindi');
                }
            }
        });

        // Codec oncelik sirasi uygula (SDP munging)
        if (this.preferredCodecs && this.preferredCodecs.length > 0) {
            this._applyCodecPriority(pc);
        }

        if (this.remoteAudio) {
            this.remoteAudio.srcObject = remoteAudioStream;
            this.remoteAudio.play().catch(err => {
                console.warn('[SipClient] Audio play hatasi:', err);
            });
        }

        // Remote video elementine bagla
        this.remoteVideo = document.getElementById('remoteVideo');
        if (this.remoteVideo && remoteVideoStream.getTracks().length > 0) {
            this.remoteVideo.srcObject = remoteVideoStream;
            this.remoteVideo.play().catch(err => {
                console.warn('[SipClient] Video play hatasi:', err);
            });
            this._notifyDotNet('OnRemoteVideoStarted', null);
        }
    },

    // ═══════════════════════════════════════════════════
    // VIDEO YONETIMI
    // ═══════════════════════════════════════════════════

    /**
     * Video'yu acar veya kapatir (toggle).
     * Aktif arama sirasinda video track ekler/cikarir.
     */
    toggleVideo: async function () {
        if (!this.currentSession) {
            console.warn('[SipClient] Aktif arama yok, video toggle yapilamaz');
            return false;
        }

        const sdh = this.currentSession.sessionDescriptionHandler;
        if (!sdh || !sdh.peerConnection) return false;

        const pc = sdh.peerConnection;

        if (this.videoEnabled) {
            // Video kapat
            this._stopLocalVideo(pc);
            this.videoEnabled = false;
            this._notifyDotNet('OnVideoToggled', false);
            console.log('[SipClient] Video kapatildi');
        } else {
            // Video ac
            try {
                const videoStream = await navigator.mediaDevices.getUserMedia({
                    video: { width: { ideal: 1280 }, height: { ideal: 720 }, frameRate: { max: 30 } }
                });

                this.localVideoStream = videoStream;
                const videoTrack = videoStream.getVideoTracks()[0];

                // PeerConnection'a video track ekle
                pc.addTrack(videoTrack, videoStream);

                // Local video preview
                this.localVideo = document.getElementById('localVideo');
                if (this.localVideo) {
                    this.localVideo.srcObject = videoStream;
                    this.localVideo.play().catch(() => {});
                }

                // Renegotiate SDP (video track eklendi)
                if (sdh.sendReinvite) {
                    await sdh.sendReinvite();
                }

                this.videoEnabled = true;
                this._notifyDotNet('OnVideoToggled', true);
                console.log('[SipClient] Video acildi');
            } catch (err) {
                console.error('[SipClient] Video acma hatasi:', err);
                this._notifyDotNet('OnVideoError', err.message || 'Kamera erisim hatasi');
                return false;
            }
        }
        return this.videoEnabled;
    },

    /**
     * Local video stream'i durdurur ve PeerConnection'dan cikarir.
     */
    _stopLocalVideo: function (pc) {
        if (this.localVideoStream) {
            this.localVideoStream.getTracks().forEach(track => track.stop());
            this.localVideoStream = null;
        }

        if (this.localVideo) {
            this.localVideo.srcObject = null;
        }

        // PeerConnection'dan video sender'i cikar
        if (pc) {
            const videoSenders = pc.getSenders().filter(s => s.track && s.track.kind === 'video');
            videoSenders.forEach(sender => {
                pc.removeTrack(sender);
            });
        }
    },

    /**
     * Video durumunu dondurur.
     */
    isVideoEnabled: function () {
        return this.videoEnabled;
    },

    /**
     * C# tarafina bildirim gonderir (DotNetObjectReference uzerinden).
     */
    _notifyDotNet: async function (methodName, data) {
        if (this.dotNetRef) {
            try {
                await this.dotNetRef.invokeMethodAsync(methodName, data);
            } catch (err) {
                console.error('[SipClient] C# callback hatasi (' + methodName + '):', err);
            }
        }
    },

    // ═══════════════════════════════════════════════════
    // CODEC PRIORITY (SDP Munging)
    // ═══════════════════════════════════════════════════

    /**
     * WebRTC transceiver codec oncelik sirasini ayarlar.
     * Tarayici destekledigi codec'ler arasindan bizim tercihlerimize gore siralar.
     * @param {RTCPeerConnection} pc
     */
    _applyCodecPriority: function (pc) {
        if (!pc || !this.preferredCodecs) return;

        try {
            const transceivers = pc.getTransceivers();
            for (const transceiver of transceivers) {
                if (transceiver.receiver && transceiver.receiver.track &&
                    transceiver.receiver.track.kind === 'audio') {

                    // setCodecPreferences destekleniyor mu?
                    if (typeof transceiver.setCodecPreferences !== 'function') {
                        console.log('[SipClient] setCodecPreferences desteklenmiyor — SDP munging atlanıyor');
                        return;
                    }

                    const capabilities = RTCRtpReceiver.getCapabilities('audio');
                    if (!capabilities || !capabilities.codecs) return;

                    const preferredOrder = this.preferredCodecs.map(c => c.toLowerCase());
                    const sorted = [...capabilities.codecs].sort((a, b) => {
                        const aName = (a.mimeType || '').split('/')[1]?.toLowerCase() || '';
                        const bName = (b.mimeType || '').split('/')[1]?.toLowerCase() || '';
                        const aIdx = preferredOrder.indexOf(aName);
                        const bIdx = preferredOrder.indexOf(bName);
                        const aPrio = aIdx >= 0 ? aIdx : 999;
                        const bPrio = bIdx >= 0 ? bIdx : 999;
                        return aPrio - bPrio;
                    });

                    transceiver.setCodecPreferences(sorted);
                    console.log('[SipClient] Codec oncelik sirasi uygulandi:', preferredOrder);
                }
            }
        } catch (err) {
            console.warn('[SipClient] Codec onceligi ayarlanamadi:', err);
        }
    },

    // ═══════════════════════════════════════════════════
    // NETWORK CHANGE DETECTION
    // ═══════════════════════════════════════════════════

    /**
     * Ag degisikligini dinler (online/offline + connection change).
     * Baglanti kesilirse SIP re-register yapar.
     */
    startNetworkDetection: function () {
        if (this._networkListenerActive) return;
        this._networkListenerActive = true;

        this._onOnline = () => {
            console.log('[SipClient] Ag baglantisi geldi — re-register...');
            if (this.registerer && !this.isRegistered) {
                setTimeout(async () => {
                    try {
                        await this.registerer.register();
                        console.log('[SipClient] Ag degisikligi sonrasi re-register baslatildi');
                    } catch (err) {
                        console.error('[SipClient] Re-register hatasi:', err);
                    }
                }, 2000); // Ag stabilize olsun
            }
        };

        this._onOffline = () => {
            console.log('[SipClient] Ag baglantisi kesildi');
            this.isRegistered = false;
            this._notifyDotNet('OnRegistrationFailed', 'Ag baglantisi kesildi');
        };

        this._onConnectionChange = () => {
            const conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
            if (conn) {
                console.log('[SipClient] Ag tipi degisti:', conn.effectiveType, 'downlink:', conn.downlink);
            }
        };

        window.addEventListener('online', this._onOnline);
        window.addEventListener('offline', this._onOffline);
        if (navigator.connection) {
            navigator.connection.addEventListener('change', this._onConnectionChange);
        }

        console.log('[SipClient] Ag degisikligi algilama baslatildi');
    },

    stopNetworkDetection: function () {
        if (!this._networkListenerActive) return;
        this._networkListenerActive = false;

        window.removeEventListener('online', this._onOnline);
        window.removeEventListener('offline', this._onOffline);
        if (navigator.connection && this._onConnectionChange) {
            navigator.connection.removeEventListener('change', this._onConnectionChange);
        }
    },

    // ═══════════════════════════════════════════════════
    // INBAND DTMF (Audio tone injection)
    // ═══════════════════════════════════════════════════

    /**
     * DTMF frekans tablosu (ITU-T Q.23)
     */
    _dtmfFrequencies: {
        '1': [697, 1209], '2': [697, 1336], '3': [697, 1477], 'A': [697, 1633],
        '4': [770, 1209], '5': [770, 1336], '6': [770, 1477], 'B': [770, 1633],
        '7': [852, 1209], '8': [852, 1336], '9': [852, 1477], 'C': [852, 1633],
        '*': [941, 1209], '0': [941, 1336], '#': [941, 1477], 'D': [941, 1633]
    },

    /**
     * Inband DTMF: Web Audio API ile dual-tone DTMF sinyali uretir.
     * Eski PBX'lerle uyumluluk icin (RFC 2833 desteklemeyen).
     * @param {string} digit - DTMF rakam (0-9, *, #, A-D)
     * @param {number} durationMs - Ton suresi (ms, varsayilan 100)
     */
    sendInbandDtmf: function (digit, durationMs) {
        if (!this.currentSession) return;
        const freqs = this._dtmfFrequencies[digit.toUpperCase()];
        if (!freqs) return;

        durationMs = durationMs || 100;

        try {
            const sdh = this.currentSession.sessionDescriptionHandler;
            if (!sdh || !sdh.peerConnection) return;

            const audioCtx = new AudioContext();
            const osc1 = audioCtx.createOscillator();
            const osc2 = audioCtx.createOscillator();
            const gainNode = audioCtx.createGain();

            osc1.type = 'sine';
            osc1.frequency.value = freqs[0];
            osc2.type = 'sine';
            osc2.frequency.value = freqs[1];
            gainNode.gain.value = 0.3; // %30 volume

            // MediaStreamDestination araciligiyla ses akisina mix
            const dest = audioCtx.createMediaStreamDestination();
            osc1.connect(gainNode);
            osc2.connect(gainNode);
            gainNode.connect(dest);

            osc1.start();
            osc2.start();

            setTimeout(() => {
                osc1.stop();
                osc2.stop();
                audioCtx.close();
            }, durationMs);

            console.log('[SipClient] Inband DTMF gonderildi:', digit, freqs);
        } catch (err) {
            console.warn('[SipClient] Inband DTMF hatasi:', err);
        }
    }
};
