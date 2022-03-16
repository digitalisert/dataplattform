#!/bin/sh

echo "Post startup"

until [ $(curl -fLSs http://localhost -o /dev/null -w '%{http_code}\n') -eq "200" ]
do
  echo "Waiting for Drupal to start..."
  sleep 1;
done

echo "Drupal started."

BOOTSTRAP=$(drush core:status --format=string bootstrap)

if [ "$BOOTSTRAP" = 'Successful' ]
then
  echo "Initializing already complete"
else
  echo "Initializing with drush"
  drush -y site:install minimal 2>&1 | sed 's/password: .*/password: *****/'
  drush theme:enable gin
  drush pm:enable config
  drush pm:enable webform
  drush -y config:set system.theme default gin
  drush -y config:set system.theme admin ''
  drush theme:uninstall stark
  echo "Initializing complete"
fi

drush -y config:import --partial
drush cache:rebuild
