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

    /**
     * SIP client'i baslatir ve register olur.
     * @param {string} wsUri - WebSocket URI (ornek: wss://sip.example.com:8443)
     * @param {string} sipUri - SIP URI (ornek: sip:1001@sip.example.com)
     * @param {string} authUser - SIP kullanici adi
     * @param {string} authPass - SIP sifresi
     * @param {string} displayName - Arayan adi
     * @param {object} dotNetRef - Blazor DotNetObjectReference (C# callback icin)
     */
    initialize: async function (wsUri, sipUri, authUser, authPass, displayName, dotNetRef) {
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

            this.userAgent = new SIP.UserAgent({
                uri: uri,
                authorizationUsername: authUser,
                authorizationPassword: authPass,
                displayName: displayName || authUser,
                transportOptions: {
                    server: wsUri
                },
                sessionDescriptionHandlerFactoryOptions: {
                    constraints: { audio: true, video: false }
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
                    constraints: { audio: true, video: false }
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
                    constraints: { audio: true, video: false }
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
     * @param {string} tone - DTMF karakteri (0-9, *, #)
     */
    sendDtmf: function (tone) {
        if (!this.currentSession || this.currentSession.state !== SIP.SessionState.Established) {
            return false;
        }

        try {
            this.currentSession.info({
                requestOptions: {
                    body: {
                        contentDisposition: 'render',
                        contentType: 'application/dtmf-relay',
                        content: 'Signal=' + tone + '\r\nDuration=160'
                    }
                }
            });
            console.log('[SipClient] DTMF gonderildi:', tone);
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
     * Uzak tarafin sesini <audio> elementine baglar.
     */
    _setupRemoteMedia: function (session) {
        const sdh = session.sessionDescriptionHandler;
        if (!sdh || !sdh.peerConnection) return;

        const pc = sdh.peerConnection;
        const remoteStream = new MediaStream();

        pc.getReceivers().forEach(receiver => {
            if (receiver.track) {
                remoteStream.addTrack(receiver.track);
            }
        });

        if (this.remoteAudio) {
            this.remoteAudio.srcObject = remoteStream;
            this.remoteAudio.play().catch(err => {
                console.warn('[SipClient] Audio play hatasi:', err);
            });
        }
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
    }
};
