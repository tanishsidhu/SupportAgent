namespace SupportAgent.Ui;

public static class UiTheme
{
    public const string BaseCss = """
    :root {
      font-family: system-ui, sans-serif;
      background: #0f172a;
      --accent: #2563eb;
      --panel: #1e293b;
      --border: #334155;
      --text: #e2e8f0;
      --muted: #94a3b8;
      --dim: #64748b;
    }
    body { max-width: 960px; margin: 0 auto; padding: 1.5rem; color: var(--text); }
    header { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1.25rem; flex-wrap: wrap; }
    h1 { margin: 0; font-size: 1.35rem; font-weight: 600; }
    .subtitle { color: var(--muted); font-size: 0.9rem; margin-top: 0.25rem; }
    .subtitle a { color: #60a5fa; text-decoration: none; }
    .subtitle a:hover { color: #93c5fd; }
    .panel { background: var(--panel); border: 1px solid var(--border); border-radius: 10px; padding: 1rem; margin-bottom: 1rem; }
    .panel h2 { margin: 0 0 0.75rem; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.06em; color: var(--dim); font-weight: 600; }
    button { cursor: pointer; border: none; border-radius: 8px; padding: 0.5rem 0.9rem; font: inherit; font-weight: 600; }
    .empty { color: var(--dim); font-style: italic; font-size: 0.85rem; }
    .footer { margin-top: 1rem; text-align: center; }
    .footer a { color: var(--dim); font-size: 0.8rem; text-decoration: none; }
    .footer a:hover { color: var(--muted); }
    #message { display: none; padding: 0.6rem 0.85rem; border-radius: 8px; margin-bottom: 1rem; font-size: 0.85rem; }
    #message.ok { display: block; background: #14532d; color: #86efac; }
    #message.err { display: block; background: #450a0a; color: #fca5a5; }
    #error { display: none; background: #450a0a; color: #fca5a5; padding: 0.6rem 0.85rem; border-radius: 8px; margin-bottom: 1rem; font-size: 0.85rem; }
    #error.show { display: block; }
    """;
}
