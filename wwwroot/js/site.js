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

// --- Toast helper with graceful fallback -------------------
function showToast(message, title, variant) {
    // Try OAT library toast if available
    if (typeof ot !== 'undefined' && ot.toast) {
        ot.toast(message, title, { variant: variant || 'info' });
        return;
    }
    // Fallback: create a temporary alert banner
    const alert = document.createElement('div');
    alert.setAttribute('role', 'alert');
    alert.setAttribute('data-variant', variant === 'danger' ? 'error' : (variant || 'success'));
    alert.innerHTML = `<strong>${title || 'Notice'}</strong> ${message}`;
    const container = document.querySelector('.main-content');
    if (container) {
        container.prepend(alert);
        setTimeout(() => {
            alert.classList.add('fade-out');
            setTimeout(() => alert.remove(), 400);
        }, 4000);
    }
}

// --- Auto-dismiss alerts elegantly ------------------------
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[role="alert"]').forEach(el => {
        // Validation summaries should not auto-dismiss
        if (el.classList.contains('validation-summary-errors')) return;
        // Empty validation summaries (no errors) should hide
        if (el.querySelector('.validation-summary-valid')) return;
        // Skip if it has no text content
        const textContent = el.textContent?.trim();
        if (!textContent) return;

        // Let user read it, then elegantly fade out
        setTimeout(() => {
            el.classList.add('fade-out');
            setTimeout(() => el.remove(), 400);
        }, 5000);
    });
});

// --- Inline AJAX adding for Dropdowns ---------------------
function inlineAdd(entityName, selectId) {
    const name = prompt(`Quick Add — Enter new ${entityName} name:`);
    if (!name || !name.trim()) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (!token) {
        showToast('Security token missing. Please refresh the page.', 'Security Error', 'danger');
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
        .then(r => {
            if (!r.ok) throw new Error(`HTTP ${r.status}`);
            return r.json();
        })
        .then(data => {
            if (data.success) {
                const sel = document.getElementById(selectId);
                if (sel) {
                    const opt = new Option(data.name, data.id, true, true);
                    sel.appendChild(opt);
                }
                showToast(`Added "${data.name}"`, 'Success', 'success');
            } else {
                showToast(data.message || 'Operation failed.', 'Error', 'danger');
            }
        })
        .catch((err) => {
            showToast('Network error. Check connection and try again.', 'Error', 'danger');
            console.error('Inline add error:', err);
        });
}

// --- Delete confirmation ----------------------------------
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('form[data-confirm]').forEach(form => {
        form.addEventListener('submit', (e) => {
            const message = form.getAttribute('data-confirm') || 'Are you sure?';
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });
});
