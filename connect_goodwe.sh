!/bin/bash

nmcli r wifi on

nmcli -f ssid dev wifi | grep Solar | sed 's/ *$//g' | head -1 | xargs -I % nmcli dev wifi connect % password "12345678"