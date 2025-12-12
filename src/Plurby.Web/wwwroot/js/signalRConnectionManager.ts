declare var signalR: any;

class SignalRConnectionManager {
    joinGroupMethod: string;
    joinGroupParamethers: string;
    leaveGroupMethod: string;
    connection: any;

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
                    } else {

                        return null;
                    }
                }
            })
            .configureLogging(signalR.LogLevel.Error)
            .build();
    }

    async registerEvents() {
        this.connection.onreconnecting(error => {
            console.assert(this.connection.state === signalR.HubConnectionState.Reconnecting);

            document.getElementById('lostConnection').classList.remove('d-none');

        });

        this.connection.onreconnected(async connectionId => {
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);

            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.joinGroupMethod, this.joinGroupParamethers);
            } else {
                await this.connection.invoke(this.joinGroupMethod);
            }


            document.getElementById('lostConnection').classList.add('d-none');
            document.getElementById('lostConnectionManualRetry').classList.add('d-none');
        });

        this.connection.onclose(async (error) => {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);

            document.getElementById('lostConnection').classList.add('d-none');
            document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
        });
    }

    async changeConnectionParamethers(joinLeaveGroupParamethers = this.joinGroupParamethers, joinGroupMethod = this.joinGroupMethod, leaveGroupMethod = this.leaveGroupMethod) {
        if (this.connection.state !== signalR.HubConnectionState.Disconnected)
            await this.stopConnection();

        this.joinGroupMethod = joinGroupMethod;
        this.joinGroupParamethers = joinLeaveGroupParamethers;
        this.leaveGroupMethod = leaveGroupMethod;

        await this.startConnection();
    }

    async startConnection() {

        try {
            await this.connection.start();
            console.assert(this.connection.state === signalR.HubConnectionState.Connected);

            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.joinGroupMethod, this.joinGroupParamethers);
            } else {
                await this.connection.invoke(this.joinGroupMethod);
            }


            document.getElementById('lostConnection').classList.add('d-none');
            document.getElementById('lostConnectionManualRetry').classList.add('d-none');
        } catch (err) {
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);


            setTimeout(() => this.startConnection(), 5000);
        }
    };

    async stopConnection() {

        try {

            if (this.joinGroupParamethers) {
                await this.connection.invoke(this.leaveGroupMethod, this.joinGroupParamethers);
            } else {
                await this.connection.invoke(this.leaveGroupMethod);
            }

            await this.connection.stop();
            console.assert(this.connection.state === signalR.HubConnectionState.Disconnected);


            document.getElementById('lostConnection').classList.remove('d-none');
            document.getElementById('lostConnectionManualRetry').classList.remove('d-none');
        } catch (err) {
            console.assert(this.connection.state !== signalR.HubConnectionState.Disconnected);


        }
    };
}