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
    // Fallback: create a temporary alert banner (XSS-safe — uses textContent)
    const alert = document.createElement('div');
    alert.setAttribute('role', 'alert');
    alert.setAttribute('data-variant', variant === 'danger' ? 'error' : (variant || 'success'));
    const strong = document.createElement('strong');
    strong.textContent = title || 'Notice';
    alert.appendChild(strong);
    alert.appendChild(document.createTextNode(' ' + message));
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
        // NEVER auto-dismiss validation errors — user needs to fix them
        if (el.classList.contains('validation-summary-errors')) return;
        if (el.getAttribute('data-variant') === 'error') return;
        // Empty validation summaries should hide immediately
        if (el.classList.contains('validation-summary-valid')) {
            el.style.display = 'none';
            return;
        }
        // Skip if it has no text content
        const textContent = el.textContent?.trim();
        if (!textContent) return;

        // Let user read it, then elegantly fade out (success/info messages only)
        setTimeout(() => {
            el.classList.add('fade-out');
            setTimeout(() => el.remove(), 400);
        }, 5000);
    });
});

// --- Prevent double form submission -----------------------
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', (e) => {
            // Skip AJAX forms and forms that use data-confirm (handled separately)
            if (form.getAttribute('data-no-loading') !== null) return;

            const submitBtn = form.querySelector('button[type="submit"]');
            if (!submitBtn) return;

            // Check if client-side validation passes before showing loading
            // jQuery Validation integration
            if (typeof jQuery !== 'undefined' && jQuery(form).valid && !jQuery(form).valid()) {
                return; // Validation failed, don't disable button
            }

            // Prevent double-click
            if (submitBtn.dataset.submitting === 'true') {
                e.preventDefault();
                return;
            }

            submitBtn.dataset.submitting = 'true';
            submitBtn.disabled = true;

            // Save original content and show loading state
            const originalHTML = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="bi bi-arrow-repeat spin-icon"></i> Processing...';
            submitBtn.classList.add('loading-btn');

            // Re-enable after 8 seconds as a safety net (in case of redirect failure)
            setTimeout(() => {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalHTML;
                submitBtn.classList.remove('loading-btn');
                delete submitBtn.dataset.submitting;
            }, 8000);
        });
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

    // Show loading toast
    showToast(`Adding "${name.trim()}"...`, 'Please wait', 'info');

    fetch(`/MasterData/Add${entityName}Ajax`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': token
        },
        body: JSON.stringify({ name: name.trim() })
    })
        .then(r => {
            // Handle 401 from MustChangePassword middleware
            if (r.status === 401) {
                return r.json().then(data => {
                    showToast(data.message || 'Please change your password first.', 'Authentication Required', 'danger');
                    if (data.redirect) {
                        setTimeout(() => window.location.href = data.redirect, 1500);
                    }
                    return null;
                });
            }
            if (r.status === 403) {
                showToast('You do not have permission to perform this action.', 'Access Denied', 'danger');
                return null;
            }
            if (!r.ok) throw new Error(`Server returned ${r.status}`);
            return r.json();
        })
        .then(data => {
            if (!data) return; // Already handled (401/403)
            if (data.success) {
                const sel = document.getElementById(selectId);
                if (sel) {
                    const opt = new Option(data.name, data.id, true, true);
                    sel.appendChild(opt);
                }
                showToast(`"${data.name}" added successfully.`, 'Added', 'success');
            } else {
                showToast(data.message || 'Operation failed. Please try again.', 'Error', 'danger');
            }
        })
        .catch((err) => {
            console.error('Inline add error:', err);
            if (err.message.includes('Failed to fetch') || err.message.includes('NetworkError')) {
                showToast('No internet connection. Please check your network and try again.', 'Network Error', 'danger');
            } else {
                showToast(`Something went wrong: ${err.message}. Please try again.`, 'Error', 'danger');
            }
        });
}

// --- Delete/Toggle confirmation with better feedback ------
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

// --- Set max date on DateFound inputs to prevent future dates ---
document.addEventListener('DOMContentLoaded', () => {
    const today = new Date().toISOString().split('T')[0];
    const dateFoundInput = document.getElementById('DateFound');
    if (dateFoundInput && dateFoundInput.type === 'date') {
        dateFoundInput.setAttribute('max', today);
    }
});

// --- Client-side file size validation ----------------------
document.addEventListener('DOMContentLoaded', () => {
    const MAX_SIZE = 10 * 1024 * 1024; // 10MB — matches server config

    document.querySelectorAll('input[type="file"]').forEach(input => {
        input.addEventListener('change', () => {
            if (input.files && input.files[0]) {
                const file = input.files[0];
                if (file.size > MAX_SIZE) {
                    const sizeMB = (file.size / (1024 * 1024)).toFixed(1);
                    showToast(`File "${file.name}" is ${sizeMB} MB. Maximum allowed is 10 MB.`, 'File Too Large', 'danger');
                    input.value = ''; // Clear the selection
                    return;
                }

                // Show photo preview for image uploads
                if (file.type.startsWith('image/') && input.id === 'PhotoFile') {
                    let previewContainer = input.parentElement.querySelector('.upload-preview');
                    if (!previewContainer) {
                        previewContainer = document.createElement('div');
                        previewContainer.className = 'upload-preview mt-2';
                        input.parentElement.appendChild(previewContainer);
                    }
                    const reader = new FileReader();
                    reader.onload = (e) => {
                        previewContainer.innerHTML = '';
                        const img = document.createElement('img');
                        img.src = e.target.result;
                        img.className = 'preview-thumb';
                        img.alt = 'Upload preview';
                        previewContainer.appendChild(img);
                    };
                    reader.readAsDataURL(file);
                }

                // Confirmation toast for successful file selection
                const sizeMB = (file.size / (1024 * 1024)).toFixed(1);
                showToast(`"${file.name}" (${sizeMB} MB) selected.`, 'File Ready', 'success');
            }
        });
    });
});

// --- Global error handler for uncaught JS errors ----------
window.addEventListener('error', (e) => {
    console.error('Uncaught error:', e.error);
    // Don't show toast for script loading errors from CDN
    if (e.filename && !e.filename.includes(window.location.hostname)) return;
    showToast('An unexpected error occurred. Please refresh the page.', 'Error', 'danger');
});

// --- Global handler for unhandled promise rejections ------
window.addEventListener('unhandledrejection', (e) => {
    console.error('Unhandled promise rejection:', e.reason);
    showToast('A background operation failed. Please try again.', 'Error', 'danger');
});
