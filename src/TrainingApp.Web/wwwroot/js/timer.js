// Rest timer interop for WorkoutFocus
window.timerInterop = {
    intervalId: null,

    start: function (seconds, dotNetRef) {
        this.stop();
        var remaining = seconds;

        this.intervalId = setInterval(function () {
            remaining--;
            if (remaining <= 0) {
                clearInterval(window.timerInterop.intervalId);
                window.timerInterop.intervalId = null;
                dotNetRef.invokeMethodAsync('OnTimerComplete');
            } else {
                dotNetRef.invokeMethodAsync('OnTimerTick', remaining);
            }
        }, 1000);
    },

    stop: function () {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }
};
