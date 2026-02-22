// ========================================================
// Lost & Found — Enterprise UI Logic
// ========================================================

// --- Sidebar Mobile Toggle --------------------------------
document.addEventListener('DOMContentLoaded', () => {
    const toggleBtn = document.querySelector('[data-sidebar-toggle]');
    const sidebar = document.querySelector('[data-sidebar]');

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', () => {
            sidebar.classList.toggle('open');
        });

        // Close when clicking outside on mobile
        document.addEventListener('click', (e) => {
            if (window.innerWidth <= 900 &&
                sidebar.classList.contains('open') &&
                !sidebar.contains(e.target) &&
                !toggleBtn.contains(e.target)) {
                sidebar.classList.remove('open');
            }
        });
    }
});

// --- Theme Toggling ---------------------------------------
function toggleTheme() {
    const root = document.documentElement;
    const isDark = root.getAttribute('data-theme') === 'dark';
    const next = isDark ? 'light' : 'dark';
    root.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
}

// --- Auto-dismiss alerts elegantly ------------------------
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[role="alert"]').forEach(el => {
        // Validation summaries should not auto-dismiss
        if (el.classList.contains('validation-summary-errors')) return;

        // Let user read it, then elegantly fade out
        setTimeout(() => {
            el.classList.add('fade-out');
            setTimeout(() => el.remove(), 400); // Wait for CSS animation
        }, 5000);
    });
});

// --- Inline AJAX adding for Dropdowns ---------------------
function inlineAdd(entityName, selectId) {
    const name = prompt(`Quick Add — Enter new ${entityName} name:`);
    if (!name || !name.trim()) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        ot.toast('Security token missing. Please refresh.', 'Security Error', { variant: 'danger' });
        return;
    }

    fetch(`/MasterData/Add${entityName}Ajax`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ name: name.trim() })
    })
        .then(r => r.json())
        .then(data => {
            if (data.success) {
                const sel = document.getElementById(selectId);
                if (sel) {
                    const opt = new Option(data.name, data.id, true, true);
                    sel.appendChild(opt);
                }
                ot.toast(`Added "${data.name}"`, 'Success', { variant: 'success' });
            } else {
                ot.toast(data.message || 'Operation failed.', 'Error', { variant: 'danger' });
            }
        })
        .catch(() => {
            ot.toast('Network error. Check connection.', 'Error', { variant: 'danger' });
        });
}
