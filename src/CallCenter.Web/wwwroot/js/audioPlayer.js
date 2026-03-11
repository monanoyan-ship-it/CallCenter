window.audioPlayer = {
    _instances: {},

    play: function (audioElementId, url, authToken) {
        var self = this;
        var existing = self._instances[audioElementId];
        if (existing && existing.objectUrl) {
            existing.audio.play();
            return Promise.resolve(true);
        }

        // Remove stale audio element if exists
        var old = document.getElementById(audioElementId);
        if (old) old.remove();

        var audio = document.createElement('audio');
        audio.id = audioElementId;
        audio.style.display = 'none';
        document.body.appendChild(audio);

        if (!url) return Promise.resolve(false);

        return fetch(url, {
            headers: { 'Authorization': 'Bearer ' + authToken }
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Stream failed: ' + response.status);
            return response.blob();
        })
        .then(function (blob) {
            var objectUrl = URL.createObjectURL(blob);
            audio.src = objectUrl;
            self._instances[audioElementId] = { audio: audio, objectUrl: objectUrl };
            return audio.play().then(function () { return true; });
        })
        .catch(function (err) {
            console.error('[audioPlayer] Error:', err);
            return false;
        });
    },

    pause: function (audioElementId) {
        var instance = this._instances[audioElementId];
        if (instance) instance.audio.pause();
    },

    stop: function (audioElementId) {
        var instance = this._instances[audioElementId];
        if (instance) {
            instance.audio.pause();
            instance.audio.removeAttribute('src');
            instance.audio.load();
            if (instance.objectUrl) {
                URL.revokeObjectURL(instance.objectUrl);
            }
            instance.audio.remove();
            delete this._instances[audioElementId];
        }
        // Cleanup any orphaned element
        var el = document.getElementById(audioElementId);
        if (el) el.remove();
    },

    seek: function (audioElementId, position) {
        var instance = this._instances[audioElementId];
        if (instance) instance.audio.currentTime = position;
    },

    getDuration: function (audioElementId) {
        var instance = this._instances[audioElementId];
        if (instance && !isNaN(instance.audio.duration)) return instance.audio.duration;
        return 0;
    },

    getCurrentTime: function (audioElementId) {
        var instance = this._instances[audioElementId];
        if (instance) return instance.audio.currentTime;
        return 0;
    },

    getState: function (audioElementId, sliderId) {
        var instance = this._instances[audioElementId];
        if (!instance) return { playing: false, currentTime: 0, duration: 0, ended: false };
        var ct = instance.audio.currentTime || 0;
        var dur = isNaN(instance.audio.duration) ? 0 : instance.audio.duration;
        if (sliderId) {
            var slider = document.getElementById(sliderId);
            if (slider) {
                slider.max = dur;
                slider.value = ct;
            }
        }
        return {
            playing: !instance.audio.paused && !instance.audio.ended,
            currentTime: ct,
            duration: dur,
            ended: instance.audio.ended
        };
    },

    setVolume: function (audioElementId, volume) {
        var instance = this._instances[audioElementId];
        if (instance) instance.audio.volume = Math.max(0, Math.min(1, volume));
    }
};
