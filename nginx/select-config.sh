#!/bin/sh
set -eu

domain="${DOMAIN_NAME:?DOMAIN_NAME must be set}"

if [ -s "/etc/letsencrypt/live/${domain}/fullchain.pem" ] &&
   [ -s "/etc/letsencrypt/live/${domain}/privkey.pem" ]; then
    config="https.conf"
else
    config="http.conf"
fi

sed "s/__DOMAIN_NAME__/${domain}/g" "/opt/nginx-config/${config}" \
    > /etc/nginx/conf.d/default.conf
