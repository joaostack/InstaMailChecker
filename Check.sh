#!/usr/bin/env bash

figlet "InstaUserCHK" | lolcat

if [[ -z "$1" ]]; then
	echo "Missing username/mail as parameter!"
	exit 0;
fi

## CHECK Tools
[[ $(type -P jq) ]] || { echo "Missing JQ, install it!" ; exit 1 ; }
[[ $(type -P curl) ]] || { echo "Missing CURL, install it!" ; exit 1 ; }

## GET CSRF TOKEN
csrfToken=$(curl \
	-s -L \
	-H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36" \
	"https://www.instagram.com/" |
	grep --color -oE '"csrf_token":"[^"]*"' | cut -d ":" -f2 | sed s'/"//'g)

echo "CSRF TOKEN => $csrfToken" | lolcat

## GET JAZOEST
jazoest=$(curl \
	-s -L \
	-H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36" \
	"https://www.instagram.com" |
	grep --color -m 1 -oIE "jazoest[^\"]*" | cut -d '=' -f2)

echo "JAZOEST => $jazoest" | lolcat

echo

## COMMUNICATE WITH API
apiResponse=$(curl \
	-s "https://www.instagram.com/api/v1/web/accounts/account_recovery_send_ajax/" \
	-X POST \
	-H "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36" \
	-H "Referer: https://www.instagram.com/accounts/password/reset/" \
	-H "Origin: https://www.instagram.com" \
	-H "sec-ch-ua-platform-version: \"10.0.0\""\
	-H "X-ASBD-ID: 359341" \
	-H "X-CSRFToken: $csrfToken" \
	-H "X-Instagram-AJAX: 1026552192" \
	-H "X-Web-Session-ID: shsyhz:8firoq:3j4uff" \
	-H "X-IG-App-ID: 936619743392459" \
	-H "Content-Type: application/x-www-form-urlencoded" \
	-H "X-Requested-With: XMLHttpRequest" \
	-H "X-IG-WWW-Claim: 0" \
	--data "email_or_username=$1&jazoest=$jazoest")

echo "$apiResponse" | jq
