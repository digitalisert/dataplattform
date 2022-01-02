#!/bin/sh

drush views:execute resource rest_export_1 > /opt/drupal/export/resource.json
drush views:execute property rest_export_1 > /opt/drupal/export/property.json
