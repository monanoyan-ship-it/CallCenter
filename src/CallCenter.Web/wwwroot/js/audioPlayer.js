window.audioPlayer = {
    _instances: {},

    play: function (audioElementId, url, authToken) {
        var self = this;
        var existing = self._instances[audioElementId];
        if (existing && existing.objectUrl) {
            // Resume paused playback
            existing.audio.play();
            return;
        }

        var audio = document.getElementById(audioElementId);
        if (!audio) {
            audio = document.createElement('audio');
            audio.id = audioElementId;
            audio.style.display = 'none';
            document.body.appendChild(audio);
        }

        // Fetch with auth header
        fetch(url, {
            headers: { 'Authorization': 'Bearer ' + authToken }
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Stream failed: ' + response.status);
            return response.blob();
        })
        .then(function (blob) {
            var objectUrl = URL.createObjectURL(blob);
            audio.src = objectUrl;
            audio.play();
            self._instances[audioElementId] = { audio: audio, objectUrl: objectUrl };
        })
        .catch(function (err) {
            console.error('[audioPlayer] Error:', err);
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
            instance.audio.currentTime = 0;
            if (instance.objectUrl) {
                URL.revokeObjectURL(instance.objectUrl);
            }
            delete this._instances[audioElementId];
        }
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

    getState: function (audioElementId) {
        var instance = this._instances[audioElementId];
        if (!instance) return { playing: false, currentTime: 0, duration: 0 };
        return {
            playing: !instance.audio.paused && !instance.audio.ended,
            currentTime: instance.audio.currentTime || 0,
            duration: isNaN(instance.audio.duration) ? 0 : instance.audio.duration,
            ended: instance.audio.ended
        };
    },

    setVolume: function (audioElementId, volume) {
        var instance = this._instances[audioElementId];
        if (instance) instance.audio.volume = Math.max(0, Math.min(1, volume));
    }
};
