// Runtime configuration - injected by Kubernetes ConfigMap
// This file is replaced at deployment time with environment-specific values
window.GYMHIVE_CONFIG = {
  API_GATEWAY_URL: '__API_GATEWAY_URL__', // Will be replaced by sed/envsubst in entrypoint
  ENVIRONMENT: '__ENVIRONMENT__'
};
