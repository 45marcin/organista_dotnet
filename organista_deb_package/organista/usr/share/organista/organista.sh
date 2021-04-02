#!/bin/bash

gsettings set org.gnome.desktop.session idle-delay 0
gsettings set org.gnome.desktop.lockdown disable-lock-screen true
gsettings set org.gnome.desktop.screensaver lock-enabled false
gsettings set org.gnome.desktop.screensaver idle-activation-enabled false





xset -dpms
xset s noblank
xset s off


#sudo chown -R $USER .
#sleep 7
#sudo sudo create_ap -n wlp2s0 OrganistaAP 12345678 &&
clutter -idle 1 -root &
cd "`dirname "$0"`" && ./Organista
#sudo --askpass
