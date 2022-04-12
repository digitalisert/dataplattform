FROM drupal:9.3.9-apache

RUN apt-get update -y \
    && apt-get install -y --no-install-recommends git jq unzip \
    && rm -rf /var/lib/apt/lists/*

RUN composer require \
    'drupal/admin_toolbar:3.1.0' \
    'drupal/external_entities:2.0-alpha5' \
    'drupal/gin:3.0-beta1' \
    'drupal/gin_toolbar:1.0-beta21' \
    'drupal/leaflet:2.1.23' \
    'drupal/leaflet_layers:1.1.1' \
    'drupal/migrate_plus:5.2' \
    'drupal/migrate_source_csv:3.5' \
    'drupal/migrate_tools:5.1' \
    'drupal/paragraphs:1.13' \
    'drupal/webform:^6.1' \
    'drush/drush'

RUN cp web/sites/default/default.settings.php web/sites/default/settings.php && \
    echo "include '/opt/drupal/web/sites/default/settings.local.php';" >> web/sites/default/settings.php && \
    mkdir web/sites/default/files web/sites/default/files/styles && \
    chown -R www-data:www-data web/sites && \
    mkdir -p config/sync && \
    chmod 777 config/sync && \
    mkdir -p export && \
    chmod 777 export

COPY \
    dataplattform-entrypoint.sh \
    post-start-drupal.sh \
    export.sh \
    /usr/local/bin/

RUN chmod +x \
    /usr/local/bin/dataplattform-entrypoint.sh \
    /usr/local/bin/post-start-drupal.sh \
    /usr/local/bin/export.sh

ENTRYPOINT ["/usr/local/bin/dataplattform-entrypoint.sh"]

CMD ["apache2-foreground"]

USER www-data:www-data
