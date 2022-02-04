docker-compose -f docker-compose.yml down
docker-compose -f docker-compose.yml build proxy_tor
docker-compose -f docker-compose.yml up -d
