# wns-push

Console utility for sending push notifications through Windows Push Notification Services (WNS) intended for testing and troubleshooting purposes.

## Usage

    WNSPushNotification.exe -type type -channel channel_uri -content file_path -sid package_sid -secret client_secret

OR

    WNSPushNotification.exe params_file_path

## Parameters

    -type: wns/toast, wns/badge, wns/tile, wns/raw
    -channel: Client WNS channel uri. Get this from client app.
              Looks like https://db5.notify.windows.com/?token=AwYAfhG7...uw63YQ%3d
    -content: Path to file containning notification content (xml or plain text).
    -sid: Package sid from live services.
          Looks like ms-app://s-1-15-2-379812837-1203...658-254873585
    -secret: Client secret from live services.
             Looks like SM5DkfddlfkIOldkf+shjJhlsjkJhd

OR

    params_file_path path to file containing all of the above parameters