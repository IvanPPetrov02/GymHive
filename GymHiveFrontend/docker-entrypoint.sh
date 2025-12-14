#!/bin/sh
# Inject runtime configuration from environment variables

# Replace placeholders in config.js with actual environment values
if [ -n "$API_GATEWAY_URL" ]; then
  sed -i "s|__API_GATEWAY_URL__|$API_GATEWAY_URL|g" /usr/share/nginx/html/config.js
fi

if [ -n "$ENVIRONMENT" ]; then
  sed -i "s|__ENVIRONMENT__|$ENVIRONMENT|g" /usr/share/nginx/html/config.js
fi

# Start nginx
exec nginx -g 'daemon off;'
