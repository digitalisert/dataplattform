# dataplattform
Digitalisert dataplattform

docker-compose up -d --build

docker-compose exec drupal drush user:login --uri=http://localhost
docker-compose exec drupal drush -y migrate:import --all --update

docker-compose down --remove-orphans -v
