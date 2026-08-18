/* portal.js — CRMP core JavaScript */
'use strict';

// ── Sidebar toggle ─────────────────────────────────────────────────────────
const sidebar = document.querySelector('.sidebar');
const mainArea = document.querySelector('.main-area');
const toggleBtn = document.getElementById('sidebarToggle');
if (toggleBtn) {
    toggleBtn.addEventListener('click', () => {
        sidebar.classList.toggle('collapsed');
        localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
    });
}
// Restore state
if (localStorage.getItem('sidebarCollapsed') === 'true' && sidebar) {
    sidebar.classList.add('collapsed');
}
// Mobile overlay
const overlay = document.getElementById('sidebarOverlay');
if (overlay) {
    overlay.addEventListener('click', () => sidebar.classList.remove('mobile-open'));
}

// ── Active nav link ─────────────────────────────────────────────────────────
document.querySelectorAll('.sidebar-nav a').forEach(link => {
    if (window.location.pathname.toLowerCase().includes(
        new URL(link.href, window.location.origin).pathname.toLowerCase()
    )) link.classList.add('active');
});

// ── Notification Bell ──────────────────────────────────────────────────────
const notifBellBtn = document.getElementById('notifBellBtn');
const notifDropdown = document.getElementById('notifDropdown');
if (notifBellBtn && notifDropdown) {
    notifBellBtn.addEventListener('click', e => {
        e.stopPropagation();
        notifDropdown.classList.toggle('open');
        if (notifDropdown.classList.contains('open')) loadNotifications();
    });
    document.addEventListener('click', () => notifDropdown.classList.remove('open'));
    notifDropdown.addEventListener('click', e => e.stopPropagation());
    // Poll count every 60s
    updateNotifCount();
    setInterval(updateNotifCount, 60000);
}

function updateNotifCount() {
    fetch('/Handlers/NotificationPoll.ashx?action=count')
        .then(r => r.json()).then(d => {
            const badge = document.getElementById('notifCount');
            if (badge) {
                badge.textContent = d.count || '';
                badge.style.display = d.count > 0 ? 'flex' : 'none';
            }
        }).catch(() => {});
}

function loadNotifications() {
    fetch('/Handlers/NotificationPoll.ashx?action=list')
        .then(r => r.json()).then(d => {
            const list = document.getElementById('notifList');
            if (!list) return;
            if (!d.items || d.items.length === 0) {
                list.innerHTML = '<div class="empty-state" style="padding:30px 16px"><div class="empty-state-icon">🔔</div><div class="empty-state-title">All caught up!</div></div>';
                return;
            }
            list.innerHTML = d.items.map(n => `
                <div class="notif-item ${n.isRead ? '' : 'unread'}" onclick="goNotif(${n.notifId},'${encodeURIComponent(n.link)}')">
                  <div class="notif-item-icon">🔔</div>
                  <div class="notif-item-text">
                    <div class="notif-item-title">${escHtml(n.title)}</div>
                    <div class="notif-item-msg">${escHtml(n.message)}</div>
                    <div class="notif-item-time">${n.timeAgo}</div>
                  </div>
                </div>`).join('');
        }).catch(() => {});
}

function goNotif(id, encodedLink) {
    fetch(`/Handlers/NotificationPoll.ashx?action=read&id=${id}`).catch(() => {});
    const link = decodeURIComponent(encodedLink);
    if (link) window.location.href = link;
}

function markAllRead() {
    fetch('/Handlers/NotificationPoll.ashx?action=readall').then(() => {
        updateNotifCount();
        loadNotifications();
    });
}

// ── Role Switcher ──────────────────────────────────────────────────────────
const roleBadge = document.getElementById('roleBadge');
const roleDropdown = document.getElementById('roleDropdown');
if (roleBadge && roleDropdown) {
    roleBadge.addEventListener('click', e => {
        e.stopPropagation();
        roleDropdown.classList.toggle('open');
    });
    document.addEventListener('click', () => roleDropdown.classList.remove('open'));
    roleDropdown.addEventListener('click', e => e.stopPropagation());
}

function switchRole(userRoleId, divisionId) {
    var f = document.createElement('form');
    f.method = 'post';
    f.action = '/Handlers/RoleSwitch.ashx';
    
    var i1 = document.createElement('input');
    i1.type = 'hidden'; i1.name = 'roleId'; i1.value = userRoleId;
    f.appendChild(i1);
    
    var i2 = document.createElement('input');
    i2.type = 'hidden'; i2.name = 'divId'; i2.value = divisionId || '';
    f.appendChild(i2);
    
    var i3 = document.createElement('input');
    i3.type = 'hidden'; i3.name = 'returnUrl'; i3.value = window.location.pathname + window.location.search;
    f.appendChild(i3);
    
    document.body.appendChild(f);
    f.submit();
}

// ── Toast Notifications ────────────────────────────────────────────────────
function showToast(message, type = 'success', duration = 4000) {
    let container = document.getElementById('toast-container');
    if (!container) {
        container = document.createElement('div');
        container.id = 'toast-container';
        document.body.appendChild(container);
    }
    const icons = { success: '✓', error: '✕', warning: '⚠', info: 'ℹ' };
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `<span class="toast-icon">${icons[type] || icons.info}</span><span class="toast-msg">${escHtml(message)}</span>`;
    container.appendChild(toast);
    setTimeout(() => { toast.style.opacity = '0'; toast.style.transform = 'translateY(10px)'; toast.style.transition = '.3s'; setTimeout(() => toast.remove(), 300); }, duration);
}

// ── Modal helpers ──────────────────────────────────────────────────────────
function openModal(id) {
    document.getElementById(id).style.display = 'flex';
    document.body.style.overflow = 'hidden';
}
function closeModal(id) {
    document.getElementById(id).style.display = 'none';
    document.body.style.overflow = '';
}
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
        document.querySelectorAll('.modal-overlay').forEach(m => {
            if (m.style.display !== 'none') closeModal(m.id);
        });
    }
});

// ── Checkbox — select all ──────────────────────────────────────────────────
function initSelectAll(allCheckId, rowCheckClass) {
    const allCheck = document.getElementById(allCheckId);
    if (!allCheck) return;
    allCheck.addEventListener('change', () => {
        document.querySelectorAll('.' + rowCheckClass).forEach(c => c.checked = allCheck.checked);
        updateBulkBar();
    });
    document.querySelectorAll('.' + rowCheckClass).forEach(c => {
        c.addEventListener('change', () => {
            allCheck.indeterminate = false;
            const checks = document.querySelectorAll('.' + rowCheckClass);
            const all = [...checks].every(x => x.checked);
            const none = [...checks].every(x => !x.checked);
            allCheck.checked = all;
            allCheck.indeterminate = !all && !none;
            updateBulkBar();
        });
    });
}

function getSelectedIds(rowCheckClass) {
    return [...document.querySelectorAll('.' + rowCheckClass + ':checked')].map(c => c.value);
}

function updateBulkBar() {
    const bar = document.getElementById('bulkActionBar');
    if (!bar) return;
    const ids = getSelectedIds('row-check');
    bar.style.display = ids.length > 0 ? 'flex' : 'none';
    const countEl = bar.querySelector('.bulk-count');
    if (countEl) countEl.textContent = `${ids.length} selected`;
}

// ── Utility ────────────────────────────────────────────────────────────────
function escHtml(str) {
    if (!str) return '';
    return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function confirm2(message, callback) {
    if (window.confirm(message)) callback();
}

// Auto-submit filter on select change
document.querySelectorAll('[data-autosubmit]').forEach(el => {
    el.addEventListener('change', () => el.closest('form').submit());
});

// ── SLA Countdown timers ───────────────────────────────────────────────────
document.querySelectorAll('[data-sla-deadline]').forEach(el => {
    const deadline = new Date(el.dataset.slaDeadline);
    const startedAt = new Date(el.dataset.submittedAt);
    updateSlaDisplay(el, deadline, startedAt);
    setInterval(() => updateSlaDisplay(el, deadline, startedAt), 60000);
});

function updateSlaDisplay(el, deadline, submittedAt) {
    const now = new Date();
    const totalMs = deadline - submittedAt;
    const elapsedMs = now - submittedAt;
    const remainingMs = deadline - now;
    const pct = Math.min(100, Math.round(elapsedMs / totalMs * 100));

    const bar = el.querySelector('.sla-bar-fill');
    if (bar) bar.style.width = pct + '%';

    const label = el.querySelector('.sla-time-label');
    if (label) {
        if (remainingMs < 0) {
            label.textContent = 'BREACHED';
        } else {
            const h = Math.floor(remainingMs / 3600000);
            const m = Math.floor((remainingMs % 3600000) / 60000);
            label.textContent = h > 0 ? `${h}h ${m}m remaining` : `${m}m remaining`;
        }
    }

    // Update CSS class
    const wrap = el;
    wrap.classList.remove('sla-ok','sla-warning','sla-breached');
    if (remainingMs < 0) wrap.classList.add('sla-breached');
    else if (pct >= 75) wrap.classList.add('sla-warning');
    else wrap.classList.add('sla-ok');
}

// ── Auto-dismiss announcements ─────────────────────────────────────────────
document.querySelectorAll('.ann-dismiss').forEach(btn => {
    btn.addEventListener('click', () => {
        const banner = btn.closest('.ann-banner');
        if (banner) { banner.style.opacity = '0'; banner.style.transition = '.3s'; setTimeout(() => banner.remove(), 300); }
    });
});
