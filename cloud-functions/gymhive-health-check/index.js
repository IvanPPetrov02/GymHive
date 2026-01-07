// GCP Cloud Functions (Gen2) - Node.js 20
// HTTP-triggered health checker for GymHive ingress.
//
// Env vars:
// - BASE_URL: e.g. https://gymhive.34.8.235.214.nip.io
// - TIMEOUT_MS (optional): per-request timeout; default 5000
// - ENDPOINTS (optional): comma-separated paths; defaults to common GymHive paths

const DEFAULT_ENDPOINTS = [
  "/",
  "/api/health",
  "/api/auth/health",
  "/api/gyms/health",
  "/api/memberships/health",
  "/api/notifications/health",
  "/api/workouts/health"
];

function getBaseUrl() {
  const baseUrl = (process.env.BASE_URL || "").trim();
  if (!baseUrl) {
    throw new Error("BASE_URL env var is required (e.g. https://gymhive.<IP>.nip.io)");
  }

  return baseUrl.endsWith("/") ? baseUrl.slice(0, -1) : baseUrl;
}

function getEndpoints() {
  const endpointsRaw = (process.env.ENDPOINTS || "").trim();
  if (!endpointsRaw) {
    return DEFAULT_ENDPOINTS;
  }

  // Support comma or semicolon-separated lists.
  // Note: semicolons are convenient with `gcloud --set-env-vars` because commas separate key/value pairs.
  return endpointsRaw
    .split(/[;,]/)
    .map((s) => s.trim())
    .filter(Boolean)
    // gcloud escaping sometimes leaves values like "\/health".
    .map((p) => p.replaceAll("\\/", "/"))
    .map((p) => (p.startsWith("/") ? p : `/${p}`));
}

function getTimeoutMs() {
  const parsed = Number.parseInt(process.env.TIMEOUT_MS || "", 10);
  if (Number.isFinite(parsed) && parsed > 0) return parsed;
  return 5000;
}

async function fetchWithTimeout(url, timeoutMs) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);

  try {
    const start = Date.now();
    const response = await fetch(url, {
      method: "GET",
      redirect: "manual",
      signal: controller.signal
    });

    const durationMs = Date.now() - start;
    return {
      ok: response.ok,
      status: response.status,
      durationMs,
      redirected: response.status >= 300 && response.status < 400,
      location: response.headers.get("location") || undefined
    };
  } finally {
    clearTimeout(timeout);
  }
}

exports.gymhiveHealthCheck = async (req, res) => {
  try {
    if (req.method !== "GET" && req.method !== "HEAD") {
      res.status(405).json({ error: "Method not allowed" });
      return;
    }

    const baseUrl = getBaseUrl();
    const endpoints = getEndpoints();
    const timeoutMs = getTimeoutMs();

    const checks = await Promise.all(
      endpoints.map(async (path) => {
        const url = `${baseUrl}${path}`;
        try {
          const result = await fetchWithTimeout(url, timeoutMs);
          return { path, url, ...result };
        } catch (error) {
          return {
            path,
            url,
            ok: false,
            status: 0,
            durationMs: timeoutMs,
            error: error?.name === "AbortError" ? "timeout" : (error?.message || "unknown error")
          };
        }
      })
    );

    const allOk = checks.every((c) => c.ok);

    const payload = {
      status: allOk ? "healthy" : "unhealthy",
      checkedAt: new Date().toISOString(),
      baseUrl,
      timeoutMs,
      checks
    };

    // Cloud Logging will pick this up.
    console.log(JSON.stringify(payload));

    res.status(allOk ? 200 : 503).json(payload);
  } catch (error) {
    console.error(error);
    res.status(500).json({
      status: "error",
      message: error?.message || "Unknown error"
    });
  }
};
