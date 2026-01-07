// GCP Cloud Functions (Gen2) - Node.js 20
// Receives ArgoCD notifications (webhook) and sends an email summary.
//
// Env vars:
// - SENDGRID_API_KEY (required)
// - TO_EMAIL (optional, default: ivan.p.petrov02@gmail.com)
// - FROM_EMAIL (required by SendGrid; must be verified sender)
// - WEBHOOK_TOKEN (required) - shared secret; must match X-GymHive-Webhook-Token header

const sgMail = require("@sendgrid/mail");

function requiredEnv(name) {
  const value = (process.env[name] || "").trim();
  if (!value) throw new Error(`${name} env var is required`);
  return value;
}

function getOptionalEnv(name, fallback) {
  const value = (process.env[name] || "").trim();
  return value || fallback;
}

function safeJson(value) {
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
}

function extractArgoSummary(body) {
  // Support either:
  // 1) Our custom payload: { app, revision, repoURL, message, resources: [...] }
  // 2) Full ArgoCD app object under { app: {...} }

  const app = body?.app || body?.application || body?.payload?.app;

  const appName =
    body?.appName ||
    app?.metadata?.name ||
    body?.name ||
    "unknown-app";

  const revision =
    body?.revision ||
    app?.status?.operationState?.syncResult?.revision ||
    app?.status?.sync?.revision ||
    "unknown";

  const repoURL =
    body?.repoURL ||
    app?.spec?.source?.repoURL ||
    app?.spec?.sources?.[0]?.repoURL ||
    "unknown";

  const syncStatus = app?.status?.sync?.status || body?.syncStatus;
  const healthStatus = app?.status?.health?.status || body?.healthStatus;
  const phase = app?.status?.operationState?.phase || body?.phase;

  const resources =
    body?.resources ||
    app?.status?.operationState?.syncResult?.resources ||
    [];

  return {
    appName,
    revision,
    repoURL,
    syncStatus,
    healthStatus,
    phase,
    resources
  };
}

exports.argocdSyncEmail = async (req, res) => {
  try {
    if (req.method !== "POST") {
      res.status(405).json({ error: "Method not allowed" });
      return;
    }

    const token = (req.get("X-GymHive-Webhook-Token") || "").trim();
    const expectedToken = requiredEnv("WEBHOOK_TOKEN").trim();
    if (!token || token !== expectedToken) {
      const debugAuth = (process.env.DEBUG_AUTH || "").trim().toLowerCase() === "true";
      res.status(401).json({
        status: "error",
        message: "Unauthorized",
        ...(debugAuth
          ? {
              receivedTokenPresent: Boolean(token),
              receivedTokenLength: token.length,
              expectedTokenLength: expectedToken.length
            }
          : {})
      });
      return;
    }

    const apiKey = requiredEnv("SENDGRID_API_KEY");
    const toEmail = getOptionalEnv("TO_EMAIL", "ivan.p.petrov02@gmail.com");
    const fromEmail = requiredEnv("FROM_EMAIL");

    sgMail.setApiKey(apiKey);

    const body = req.body || {};
    const summary = extractArgoSummary(body);

    const subject = `[GymHive][ArgoCD] Synced: ${summary.appName} @ ${String(summary.revision).slice(0, 12)}`;

    const resourcesLines = Array.isArray(summary.resources)
      ? summary.resources
          .slice(0, 50)
          .map((r) => {
            const kind = r?.kind || "?";
            const name = r?.name || "?";
            const namespace = r?.namespace || "";
            const status = r?.status || r?.syncStatus || "";
            const ns = namespace ? ` (${namespace})` : "";
            return `- ${kind}/${name}${ns} ${status ? `=> ${status}` : ""}`.trim();
          })
          .join("\n")
      : "";

    const text = [
      `ArgoCD sync event received`,
      ``,
      `App: ${summary.appName}`,
      `Revision: ${summary.revision}`,
      `Repo: ${summary.repoURL}`,
      summary.syncStatus ? `Sync: ${summary.syncStatus}` : null,
      summary.healthStatus ? `Health: ${summary.healthStatus}` : null,
      summary.phase ? `Phase: ${summary.phase}` : null,
      ``,
      resourcesLines ? `Resources:\n${resourcesLines}` : null,
      ``,
      `Raw payload (truncated):`,
      safeJson(body).slice(0, 8000)
    ]
      .filter(Boolean)
      .join("\n");

    await sgMail.send({
      to: toEmail,
      from: fromEmail,
      subject,
      text
    });

    console.log(
      JSON.stringify({
        status: "sent",
        toEmail,
        app: summary.appName,
        revision: summary.revision
      })
    );

    res.status(200).json({ status: "sent" });
  } catch (error) {
    console.error(error);
    res.status(500).json({
      status: "error",
      message: error?.message || "Unknown error"
    });
  }
};
