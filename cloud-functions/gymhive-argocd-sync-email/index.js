// GCP Cloud Functions (Gen2) - Node.js 20
// Receives ArgoCD notifications (webhook) and sends an email summary.
//
// Env vars:
// - SENDGRID_API_KEY (required)
// - TO_EMAIL (optional fallback, default: ivan.p.petrov02@gmail.com)
// - FROM_EMAIL (required by SendGrid; must be verified sender)
// - WEBHOOK_TOKEN (required) - shared secret; must match X-GymHive-Webhook-Token header
// - ADMIN_EMAILS_URL (required) - e.g. https://gymhive.<IP>.nip.io/api/auth/admin-emails
// - ADMIN_EMAILS_TOKEN (required) - sent as X-GymHive-AdminEmails-Token
// - SENDGRID_TEMPLATE_ID (optional) - SendGrid Dynamic Template ID (uses dynamicTemplateData)
// - INCLUDE_RAW_PAYLOAD (optional, default false) - include raw JSON payload in email

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

function getOptionalBoolEnv(name, fallback) {
  const raw = String(process.env[name] || "").trim().toLowerCase();
  if (!raw) return fallback;
  return raw === "true" || raw === "1" || raw === "yes" || raw === "y";
}

function normalizeResources(resources) {
  const list = Array.isArray(resources) ? resources : [];
  return list.slice(0, 50).map((r) => {
    const kind = String(r?.kind || "?").trim();
    const name = String(r?.name || "?").trim();
    const namespace = String(r?.namespace || "").trim();
    const status = String(r?.status || r?.syncStatus || "").trim();
    return { kind, name, namespace, status };
  });
}

function escapeHtml(text) {
  return String(text)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function buildEmailContent(summary, body) {
  const resources = normalizeResources(summary.resources);
  const revisionShort = String(summary.revision || "").slice(0, 12);
  const includeRaw = getOptionalBoolEnv("INCLUDE_RAW_PAYLOAD", false);

  const lines = [];
  lines.push("ArgoCD sync update");
  lines.push("");
  lines.push(`App: ${summary.appName}`);
  lines.push(`Revision: ${summary.revision}`);
  lines.push(`Repo: ${summary.repoURL}`);
  if (summary.syncStatus) lines.push(`Sync: ${summary.syncStatus}`);
  if (summary.healthStatus) lines.push(`Health: ${summary.healthStatus}`);
  if (summary.phase) lines.push(`Phase: ${summary.phase}`);
  lines.push("");
  if (resources.length) {
    lines.push("Changed resources:");
    for (const r of resources) {
      const ns = r.namespace ? ` (${r.namespace})` : "";
      const status = r.status ? ` => ${r.status}` : "";
      lines.push(`- ${r.kind}/${r.name}${ns}${status}`.trim());
    }
  }
  if (includeRaw) {
    lines.push("");
    lines.push("Raw payload (truncated):");
    lines.push(safeJson(body).slice(0, 8000));
  }

  const htmlResources = resources.length
    ? `
      <h3>Changed resources</h3>
      <ul>
        ${resources
          .map((r) => {
            const ns = r.namespace ? ` <span>(${escapeHtml(r.namespace)})</span>` : "";
            const st = r.status ? ` <strong>&rarr; ${escapeHtml(r.status)}</strong>` : "";
            return `<li><span>${escapeHtml(r.kind)}/${escapeHtml(r.name)}</span>${ns}${st}</li>`;
          })
          .join("\n")}
      </ul>`
    : "";

  const htmlRaw = includeRaw
    ? `
      <h3>Raw payload (truncated)</h3>
      <pre style="white-space: pre-wrap;">${escapeHtml(safeJson(body).slice(0, 8000))}</pre>`
    : "";

  const html = `
    <div>
      <h2>ArgoCD sync update</h2>
      <p><strong>App:</strong> ${escapeHtml(summary.appName)}</p>
      <p><strong>Revision:</strong> ${escapeHtml(summary.revision)} <span style="opacity:0.7">(${escapeHtml(revisionShort)})</span></p>
      <p><strong>Repo:</strong> ${escapeHtml(summary.repoURL)}</p>
      ${summary.syncStatus ? `<p><strong>Sync:</strong> ${escapeHtml(summary.syncStatus)}</p>` : ""}
      ${summary.healthStatus ? `<p><strong>Health:</strong> ${escapeHtml(summary.healthStatus)}</p>` : ""}
      ${summary.phase ? `<p><strong>Phase:</strong> ${escapeHtml(summary.phase)}</p>` : ""}
      ${htmlResources}
      ${htmlRaw}
    </div>
  `.trim();

  return {
    resources,
    revisionShort,
    includeRaw,
    text: lines.filter(Boolean).join("\n"),
    html
  };
}

async function fetchAdminEmails() {
  const url = requiredEnv("ADMIN_EMAILS_URL");
  const token = requiredEnv("ADMIN_EMAILS_TOKEN");

  const response = await fetch(url, {
    method: "GET",
    headers: {
      "X-GymHive-AdminEmails-Token": token
    }
  });

  if (!response.ok) {
    const text = await response.text().catch(() => "");
    throw new Error(`Failed to fetch admin emails (${response.status}): ${text.slice(0, 300)}`);
  }

  const data = await response.json();
  const emails = Array.isArray(data?.emails) ? data.emails : [];
  return emails
    .map((e) => String(e || "").trim())
    .filter(Boolean);
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
    const fallbackToEmail = getOptionalEnv("TO_EMAIL", "ivan.p.petrov02@gmail.com");
    const fromEmail = requiredEnv("FROM_EMAIL");

    sgMail.setApiKey(apiKey);

    const body = req.body || {};
    const summary = extractArgoSummary(body);

    let recipients = [];
    try {
      recipients = await fetchAdminEmails();
    } catch (e) {
      console.error("Failed to resolve admin emails; falling back to TO_EMAIL", e);
      recipients = [fallbackToEmail];
    }

    // Ensure at least one recipient.
    if (!recipients.length) {
      recipients = [fallbackToEmail];
    }

    const subject = `[GymHive][ArgoCD] Synced: ${summary.appName} @ ${String(summary.revision).slice(0, 12)}`;

    const templateId = (process.env.SENDGRID_TEMPLATE_ID || "").trim();
    const content = buildEmailContent(summary, body);

    if (templateId) {
      await sgMail.send({
        to: recipients,
        from: fromEmail,
        subject,
        templateId,
        dynamicTemplateData: {
          appName: summary.appName,
          revision: summary.revision,
          revisionShort: content.revisionShort,
          repoURL: summary.repoURL,
          syncStatus: summary.syncStatus || "",
          healthStatus: summary.healthStatus || "",
          phase: summary.phase || "",
          changedResources: content.resources,
          changedCount: content.resources.length
        }
      });
    } else {
      await sgMail.send({
        to: recipients,
        from: fromEmail,
        subject,
        text: content.text,
        html: content.html
      });
    }

    res.status(200).json({ status: "sent" });
  } catch (error) {
    console.error(error);
    res.status(500).json({
      status: "error",
      message: error?.message || "Unknown error"
    });
  }
};
