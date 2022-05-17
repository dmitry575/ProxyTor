#!/bin/bash
#making script to stop on 1st error
set -e

# Original script from
# http://blog.databigbang.com/distributed-scraping-with-multiple-tor-circuits/

# if defined TOR_INSTANCE env variable sets the number of tor instances (default 10)
TOR_INSTANCES=${TOR_INSTANCES:=10 }

echo "tor instances: $TOR_INSTANCES"
# if defined TOR_OPTIONSE env variable can be used to add options to TOR
TOR_OPTIONS=${TOR_OPTIONS:=''}

# default base port
base_socks_port=${TOR_PORT_BASE:=9050}

echo "tor socks base port: $base_socks_port"

dir_data="/tmp/multitor.$$"

# Create data directory if it doesn't exist
if [ ! -d $dir_data ]; then
        mkdir $dir_data
fi


if [ ! $TOR_INSTANCES ] || [ $TOR_INSTANCES -lt 1 ]; then
    echo "Please supply an instance count"
    exit 1
fi

for i in $(seq $TOR_INSTANCES)
do
        j=$((i+1))

        socks_port=$((base_socks_port+i))

        if [ ! -d "$dir_data/tor$i" ]; then
                echo "Creating directory $dir_data/tor$i"
                mkdir "$dir_data/tor$i" && chmod -R 700 "$dir_data/tor$i"
        fi

        # Take into account that authentication for the control port is disabled. Must be used in secure and controlled environments
        echo "Running: tor --RunAsDaemon 1 --CookieAuthentication 0 --HashedControlPassword \"\" --PidFile tor$i.pid --SocksPort 0.0.0.0:$socks_port --DataDirectory $dir_data/tor$i -f /etc/tor/torrc"

        tor --RunAsDaemon 1 --CookieAuthentication 0 --HashedControlPassword "" --PidFile $dir_data/tor$i/tor$i.pid --SocksPort 0.0.0.0:$socks_port --DataDirectory $dir_data/tor$i -f /etc/tor/torrc
done

# So that the container doesn't shut down, sleep this thread
sleep infinity