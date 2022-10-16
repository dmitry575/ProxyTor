docker-compose -f docker-compose.yml down
docker-compose -f docker-compose.yml build proxy_tor
#docker system prune -a
docker-compose -f docker-compose.yml up -d 
