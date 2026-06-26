namespace SupportAgent.Ui;

public static class ReviewPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>SupportAgent — Review Queue</title>
  <style>
""" + UiTheme.BaseCss + """
    .count { color: var(--muted); font-size: 0.9rem; margin-bottom: 1rem; }
    .card { background: #0f172a; border: 1px solid var(--border); border-radius: 8px; padding: 1rem 1.1rem; margin-bottom: 0.85rem; }
    .badge { display: inline-block; background: #422006; color: #fcd34d; font-weight: 600; font-size: 0.75rem; padding: 0.2rem 0.5rem; border-radius: 4px; margin-bottom: 0.65rem; }
    .meta { color: var(--muted); font-size: 0.85rem; margin: 0.25rem 0; }
    .draft { white-space: pre-wrap; background: #020617; border-left: 3px solid #3b82f6; padding: 0.75rem; margin: 0.65rem 0; color: #cbd5e1; font-size: 0.9rem; }
    .citations { color: var(--muted); font-size: 0.85rem; }
    .citations span { color: #60a5fa; margin-right: 0.5rem; }
    textarea { width: 100%; min-height: 100px; margin-top: 0.5rem; font: inherit; padding: 0.5rem; background: #020617; color: var(--text); border: 1px solid var(--border); border-radius: 6px; }
    .actions { display: flex; gap: 0.5rem; flex-wrap: wrap; margin-top: 0.75rem; }
    .approve { background: #16a34a; color: #fff; }
    .amend { background: var(--accent); color: #fff; }
    .reject { background: #dc2626; color: #fff; }
    .problems { color: #fbbf24; font-size: 0.85rem; }
    label strong { color: var(--text); font-size: 0.85rem; }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>Review Queue</h1>
      <p class="subtitle">Pending human review — nothing is auto-posted. <a href="/">← Monitor</a></p>
    </div>
  </header>

  <div id="message"></div>
  <p class="count" id="count"></p>
  <div id="queue"></div>

  <div class="footer"><a href="/">← Back to monitor</a></div>

  <script>
    const queueEl = document.getElementById('queue');
    const messageEl = document.getElementById('message');
    const countEl = document.getElementById('count');

    function showMessage(text, ok = true) {
      messageEl.textContent = text;
      messageEl.className = ok ? 'ok' : 'err';
    }

    function escapeHtml(text) {
      return String(text ?? '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    function fmt(v) { return v == null ? 'n/a' : Number(v).toFixed(2); }
    function fmtBool(v) { return v == null ? 'n/a' : (v ? 'yes' : 'no'); }

    function problems(list) {
      if (!list || list.length === 0) return '';
      return '<div class="problems">Checker problems:<ul>' +
        list.map(p => `<li>${escapeHtml(p)}</li>`).join('') + '</ul></div>';
    }

    async function loadQueue() {
      const res = await fetch('/api/queue');
      const items = await res.json();
      countEl.textContent = items.length === 0
        ? 'No drafts waiting for review.'
        : `${items.length} draft(s) pending review.`;
      if (items.length === 0) {
        queueEl.innerHTML = '<p class="empty">The agent has not staged any answerable drafts yet.</p>';
        return;
      }
      queueEl.innerHTML = items.map(item => `
        <div class="card" data-ticket="${item.ticketId}">
          <div class="badge">PENDING REVIEW</div>
          <div><strong>${item.ticketId}</strong>: ${escapeHtml(item.subject)}</div>
          <div class="meta">${escapeHtml(item.question)}</div>
          <div class="draft">${escapeHtml(item.draft.draftAnswer)}</div>
          <div class="citations">Citations: ${(item.draft.citations || []).map(c =>
            `<span>${escapeHtml(c)}</span>`).join('') || 'none'}</div>
          <div class="meta">Evidence ${fmt(item.draft.evidenceConfidence)} · Answer ${fmt(item.draft.answerConfidence)} · Checker ${fmtBool(item.draft.checkerPass)}</div>
          ${problems(item.draft.checkerProblems)}
          <label><strong>Amend answer</strong>
            <textarea id="amend-${item.ticketId}">${escapeHtml(item.draft.draftAnswer)}</textarea>
          </label>
          <div class="actions">
            <button class="approve" onclick="approve('${item.ticketId}')">Approve</button>
            <button class="amend" onclick="amend('${item.ticketId}')">Amend &amp; approve</button>
            <button class="reject" onclick="reject('${item.ticketId}')">Reject</button>
          </div>
        </div>`).join('');
    }

    async function post(path, body) {
      const res = await fetch(path, {
        method: 'POST',
        headers: body ? { 'Content-Type': 'application/json' } : {},
        body: body ? JSON.stringify(body) : undefined
      });
      if (!res.ok) {
        const err = await res.text();
        throw new Error(err || res.statusText);
      }
      return res.json();
    }

    async function approve(id) {
      try {
        await post(`/api/review/${id}/approve`);
        showMessage(`Approved ${id}.`);
        await loadQueue();
      } catch (e) { showMessage(e.message, false); }
    }

    async function amend(id) {
      const text = document.getElementById(`amend-${id}`).value.trim();
      if (!text) { showMessage('Amended answer cannot be empty.', false); return; }
      try {
        await post(`/api/review/${id}/amend`, { answer: text });
        showMessage(`Amended and approved ${id}.`);
        await loadQueue();
      } catch (e) { showMessage(e.message, false); }
    }

    async function reject(id) {
      try {
        await post(`/api/review/${id}/reject`);
        showMessage(`Rejected ${id}.`);
        await loadQueue();
      } catch (e) { showMessage(e.message, false); }
    }

    loadQueue();
    setInterval(loadQueue, 5000);
  </script>
</body>
</html>
""";
}
