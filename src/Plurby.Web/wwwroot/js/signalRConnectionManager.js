class SignalRConnectionManager {
    constructor(connectionUrl, joinGroupParamethers, joinGroupMethod, leaveGroupMethod) {
        this.joinGroupMethod = joinGroupMethod;
        this.joinGroupParamethers = joinGroupParamethers;
        this.leaveGroupMethod = leaveGroupMethod;
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(connectionUrl)
            .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: retryContext => {
                const maxReconnectionMillisecondsDelay = 60000;

                if (retryContext.elapsedMilliseconds < maxReconnectionMillisecondsDelay) {

                    return retryContext.elapsedMilliseconds;
                }
                else {

                    return null;
        }
    }
    async stopConnection() {

        try {
            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.leaveGroupMethod, this.joinGroupParamethers);
            }
            else {
                await this.connection.invoke(this.leaveGroupMethod);
            }
            await this.connection.stop();
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);

            document.getElementById('lostConnection').classList.remove('d-none');
            document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
        }
        catch (err) {
            console.assert(this.connection.state !== signalR.HubConnectionState.Disconnected);

        }
    }
}
//# sourceMappingURL=signalRConnectionManager.js.map