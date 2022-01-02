#!/bin/sh

set -e

/usr/local/bin/post-start-drupal.sh &

exec /usr/local/bin/docker-php-entrypoint "$@"
