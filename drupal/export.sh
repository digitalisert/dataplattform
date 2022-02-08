#!/bin/sh

drush views:execute resource rest_export_1 > /opt/drupal/export/resource.json
drush views:execute property rest_export_1 > /opt/drupal/export/property.json
drush views:execute webform rest_export_1 > /opt/drupal/export/webform.json

for webform in $(jq -r '.[].webform[].target_id' /opt/drupal/export/webform.json); do
  drush webform:export --exporter=json --archive-type=zip --destination=/opt/drupal/export/webform-${webform}.zip ${webform}
done
