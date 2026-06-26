namespace SupportAgent.Ui;

public static class MonitorPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>SupportAgent — Live Monitor</title>
  <style>
""" + UiTheme.BaseCss + """
    .status-line { display: flex; align-items: center; gap: 0.6rem; color: var(--muted); font-size: 0.9rem; }
    .pulse { width: 9px; height: 9px; border-radius: 50%; background: #475569; flex-shrink: 0; }
    .pulse.on { background: #22c55e; animation: pulse 1.4s infinite; }
    @keyframes pulse { 70% { box-shadow: 0 0 0 0 rgba(34,197,94,0.5); } 100% { box-shadow: 0 0 0 10px rgba(34,197,94,0); } }
    .stats { display: flex; gap: 0.6rem; margin-bottom: 1rem; flex-wrap: wrap; }
    .stat { background: var(--panel); border: 1px solid var(--border); border-radius: 8px; padding: 0.45rem 0.85rem; min-width: 90px; }
    .stat .label { color: var(--dim); font-size: 0.68rem; text-transform: uppercase; letter-spacing: 0.04em; }
    .stat .value { font-size: 1.4rem; font-weight: 700; color: #f8fafc; }
    .inbox-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 0.45rem; }
    .ticket-chip {
      border: 1px solid var(--border); border-radius: 8px; padding: 0.5rem 0.55rem;
      background: #0f172a; transition: background 0.2s;
    }
    .ticket-chip.updated { animation: flash 0.7s ease; }
    @keyframes flash { 0% { background: #1e3a5f; } 100% { background: #0f172a; } }
    .ticket-chip .id { font-weight: 700; font-size: 0.82rem; }
    .ticket-chip .subject { color: var(--dim); font-size: 0.7rem; margin: 0.15rem 0 0.35rem; line-height: 1.2; }
    .phase-badge {
      display: inline-block; font-size: 0.65rem; font-weight: 600; padding: 0.12rem 0.4rem;
      border-radius: 999px; color: #fff;
    }
    .phase-pending { background: #64748b; }
    .phase-review { background: #3b82f6; }
    .phase-routed { background: #8b5cf6; }
    .phase-escalated { background: #ef4444; }
    .phase-approved, .phase-amended { background: #22c55e; }
    .phase-rejected { background: #b45309; }
    .activity-feed { max-height: 320px; overflow-y: auto; font-size: 0.82rem; }
    .activity-item {
      padding: 0.4rem 0; border-bottom: 1px solid var(--border);
      display: grid; grid-template-columns: 64px 1fr; gap: 0.5rem; align-items: start;
    }
    .activity-item:first-child { animation: slideIn 0.3s ease; }
    @keyframes slideIn { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: none; } }
    .activity-item:last-child { border-bottom: none; }
    .activity-step { font-weight: 700; color: #60a5fa; text-transform: uppercase; font-size: 0.65rem; padding-top: 0.1rem; }
    .activity-detail { color: #cbd5e1; }
    .activity-time { color: #475569; font-size: 0.72rem; margin-top: 0.15rem; }
    .run-log {
      background: #020617; color: var(--muted); padding: 0.75rem; border-radius: 8px;
      font-family: ui-monospace, monospace; font-size: 0.72rem;
      max-height: 140px; overflow: auto; white-space: pre-wrap; min-height: 2.5rem;
      border: 1px solid #1e293b;
    }
    .run-log.active { color: var(--text); border-color: var(--border); }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>SupportAgent</h1>
      <div class="status-line">
        <div class="pulse" id="pulse"></div>
        <span id="live-label">Starting agent…</span>
      </div>
    </div>
  </header>

  <div id="error"></div>

  <div class="stats" id="stats"></div>

  <section class="panel">
    <h2>Agent log</h2>
    <pre class="run-log active" id="run-log">Agent starting…</pre>
  </section>

  <section class="panel">
    <h2>Inbox</h2>
    <div class="inbox-grid" id="inbox"></div>
  </section>

  <section class="panel">
    <h2>Activity</h2>
    <div class="activity-feed" id="activity"></div>
  </section>

  <div class="footer"><a href="/review">Review queue →</a></div>

  <script>
    const statsEl = document.getElementById('stats');
    const inboxEl = document.getElementById('inbox');
    const activityEl = document.getElementById('activity');
    const pulseEl = document.getElementById('pulse');
    const liveLabelEl = document.getElementById('live-label');
    const runLogEl = document.getElementById('run-log');
    const errorEl = document.getElementById('error');

    let lastActivityCount = 0;

    function escapeHtml(text) {
      return String(text ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    function showError(text) {
      errorEl.textContent = text;
      errorEl.className = text ? 'show' : '';
    }

    function phaseClass(phase) {
      return 'phase-' + (phase || 'pending');
    }

    function renderStats(status) {
      const done = status.routed + status.escalated + status.inReview + status.reviewed;
      statsEl.innerHTML = [
        ['Waiting', status.pendingAgent],
        ['Staged', done],
      ].map(([label, value]) =>
        `<div class="stat"><div class="label">${label}</div><div class="value">${value}</div></div>`
      ).join('');
    }

    function renderInbox(tickets) {
      inboxEl.innerHTML = tickets.map(t =>
        `<div class="ticket-chip" data-ticket="${t.ticketId}">
          <div class="id">${escapeHtml(t.ticketId)}</div>
          <div class="subject">${escapeHtml(t.subject)}</div>
          <span class="phase-badge ${phaseClass(t.phase)}">${escapeHtml(t.phaseLabel)}</span>
        </div>`
      ).join('');
    }

    function renderActivity(events) {
      if (!events.length) {
        activityEl.innerHTML = '<p class="empty">Waiting for agent activity…</p>';
        return;
      }
      activityEl.innerHTML = events.map(e =>
        `<div class="activity-item">
          <div class="activity-step">${escapeHtml(e.step)}</div>
          <div>
            <div class="activity-detail"><strong>${escapeHtml(e.ticketId)}</strong> — ${escapeHtml(e.detail)}</div>
            <div class="activity-time">${new Date(e.at).toLocaleTimeString()}</div>
          </div>
        </div>`
      ).join('');
    }

    async function loadStatus() {
      const res = await fetch('/api/agent/status');
      const status = await res.json();

      renderStats(status);
      renderInbox(status.tickets);
      renderActivity(status.recentActivity);

      if (status.agentLog) {
        runLogEl.textContent = status.agentLog;
        runLogEl.classList.add('active');
      }

      if (status.agentError) {
        showError(status.agentError);
      }

      const active = status.agentRunning || status.agentRecentlyActive;
      pulseEl.className = 'pulse' + (active ? ' on' : '');
      liveLabelEl.textContent = status.agentRunning
        ? 'Agent running…'
        : active
          ? 'Agent active'
          : status.pendingAgent > 0
            ? `${status.pendingAgent} ticket(s) waiting`
            : 'Inbox clear';

      if (status.recentActivity.length > lastActivityCount) {
        document.querySelectorAll('.ticket-chip').forEach(el => el.classList.add('updated'));
        setTimeout(() => document.querySelectorAll('.ticket-chip.updated').forEach(el => el.classList.remove('updated')), 700);
      }
      lastActivityCount = status.recentActivity.length;
    }

    loadStatus();
    setInterval(loadStatus, 1500);
  </script>
</body>
</html>
""";
}
