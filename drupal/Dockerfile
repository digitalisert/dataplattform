FROM drupal:9.1.10-apache

RUN composer require \
    'drush/drush'

RUN cp web/sites/default/default.settings.php web/sites/default/settings.php && \
    echo "include '/opt/drupal/web/sites/default/settings.local.php';" >> web/sites/default/settings.php && \
    mkdir web/sites/default/files web/sites/default/files/styles && \
    chown -R www-data:www-data web/sites && \
    mkdir -p config/sync && \
    chmod 777 config/sync
