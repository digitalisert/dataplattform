# dataplattform
Digitalisert dataplattform

dotnet build
docker-compose up -d --build

docker exec -it dataplattform_drupal_1 drush -y site:install standard --account-pass=***
