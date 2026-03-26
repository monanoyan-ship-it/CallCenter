window.audioPlayer = {
    _instances: {},

    create: function (containerId, url, authToken) {
        var self = this;
        self.destroy(containerId);

        var container = document.getElementById(containerId);
        if (!container || !url) return Promise.resolve(false);

        return fetch(url, {
            headers: { 'Authorization': 'Bearer ' + authToken }
        })
        .then(function (response) {
            if (!response.ok) throw new Error('Stream failed: ' + response.status);
            return response.blob();
        })
        .then(function (blob) {
            var objectUrl = URL.createObjectURL(blob);
            var ws = WaveSurfer.create({
                container: container,
                waveColor: '#6c757d',
                progressColor: '#0d6efd',
                cursorColor: '#0d6efd',
                cursorWidth: 2,
                height: 64,
                barWidth: 2,
                barGap: 1,
                barRadius: 2,
                normalize: true,
                dragToSeek: true,
                url: objectUrl
            });
            self._instances[containerId] = { ws: ws, objectUrl: objectUrl };
            return new Promise(function (resolve) {
                ws.on('ready', function () { resolve(true); });
                ws.on('error', function () { resolve(false); });
            });
        })
        .catch(function (err) {
            console.error('[audioPlayer] Error:', err);
            return false;
        });
    },

    play: function (containerId) {
        var instance = this._instances[containerId];
        if (instance) instance.ws.play();
    },

    pause: function (containerId) {
        var instance = this._instances[containerId];
        if (instance) instance.ws.pause();
    },

    playPause: function (containerId) {
        var instance = this._instances[containerId];
        if (instance) instance.ws.playPause();
    },

    stop: function (containerId) {
        var instance = this._instances[containerId];
        if (instance) {
            instance.ws.stop();
        }
    },

    seek: function (containerId, seconds) {
        var instance = this._instances[containerId];
        if (instance) instance.ws.setTime(seconds);
    },

    setVolume: function (containerId, volume) {
        var instance = this._instances[containerId];
        if (instance) instance.ws.setVolume(Math.max(0, Math.min(1, volume)));
    },

    getState: function (containerId) {
        var instance = this._instances[containerId];
        if (!instance) return { playing: false, currentTime: 0, duration: 0, ended: false };
        var ws = instance.ws;
        return {
            playing: ws.isPlaying(),
            currentTime: ws.getCurrentTime(),
            duration: ws.getDuration(),
            ended: ws.getCurrentTime() >= ws.getDuration() && !ws.isPlaying()
        };
    },

    onFinish: function (containerId, dotnetRef) {
        var instance = this._instances[containerId];
        if (instance) {
            instance.ws.on('finish', function () {
                dotnetRef.invokeMethodAsync('OnWaveSurferFinished');
            });
        }
    },

    destroy: function (containerId) {
        var instance = this._instances[containerId];
        if (instance) {
            instance.ws.destroy();
            if (instance.objectUrl) {
                URL.revokeObjectURL(instance.objectUrl);
            }
            delete this._instances[containerId];
        }
    }
};
