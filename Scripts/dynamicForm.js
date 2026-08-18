/* dynamicForm.js — CRMP Dynamic Form Engine */
'use strict';

/**
 * Initialised by DynamicForm.ascx with the field config JSON.
 * Handles: conditional show/hide, client-side validation, template loading.
 */
const DynamicForm = (function () {

    let _fields = [];  // Array of field config objects from server

    function init(fieldsJson) {
        try { _fields = JSON.parse(fieldsJson); } catch (e) { _fields = []; return; }
        _fields.forEach(setupField);
        runAllConditions();
    }

    function setupField(field) {
        if (!field.conditionalParentFieldId) return;

        const parentInput = getFieldInput(field.conditionalParentFieldId);
        if (!parentInput) return;

        const eventType = parentInput.tagName === 'SELECT' ? 'change'
            : parentInput.type === 'checkbox' ? 'change' : 'input';

        parentInput.addEventListener(eventType, () => evaluateCondition(field));
    }

    function runAllConditions() {
        _fields.filter(f => f.conditionalParentFieldId).forEach(evaluateCondition);
    }

    function evaluateCondition(field) {
        const parentInput = getFieldInput(field.conditionalParentFieldId);
        if (!parentInput) return;

        let parentValue = getInputValue(parentInput);
        const shouldShow = String(parentValue).toLowerCase() === String(field.conditionalShowWhenValue).toLowerCase();

        const wrapper = document.getElementById('field-wrap-' + field.fieldId);
        if (wrapper) {
            wrapper.style.display = shouldShow ? '' : 'none';
            // Disable required validation when hidden
            const input = wrapper.querySelector('input,select,textarea');
            if (input) input.required = shouldShow && field.isRequired;
        }
    }

    function getFieldInput(fieldId) {
        return document.getElementById('ff_' + fieldId) ||
               document.querySelector(`[name="ff_${fieldId}"]`);
    }

    function getInputValue(input) {
        if (!input) return '';
        if (input.type === 'checkbox') return input.checked ? 'true' : 'false';
        if (input.type === 'radio') {
            const checked = document.querySelector(`[name="${input.name}"]:checked`);
            return checked ? checked.value : '';
        }
        return input.value;
    }

    // ── Collect all field values from the form ──────────────────────────────
    function collectValues() {
        const values = {};
        _fields.forEach(field => {
            const wrap = document.getElementById('field-wrap-' + field.fieldId);
            if (wrap && wrap.style.display === 'none') return; // Hidden — skip

            const input = getFieldInput(field.fieldId);
            if (!input) return;
            values[field.fieldId] = getInputValue(input);
        });
        return values;
    }

    // ── Validate required fields ────────────────────────────────────────────
    function validate() {
        let valid = true;
        clearErrors();
        _fields.forEach(field => {
            if (!field.isRequired) return;
            const wrap = document.getElementById('field-wrap-' + field.fieldId);
            if (wrap && wrap.style.display === 'none') return;

            const input = getFieldInput(field.fieldId);
            if (!input) return;
            const val = getInputValue(input).trim();
            if (!val) {
                showError(field.fieldId, `${field.fieldLabel} is required.`);
                valid = false;
            }
        });
        return valid;
    }

    function showError(fieldId, message) {
        const wrap = document.getElementById('field-wrap-' + fieldId);
        if (!wrap) return;
        const input = wrap.querySelector('input,select,textarea');
        if (input) input.classList.add('error');
        let errEl = wrap.querySelector('.form-error');
        if (!errEl) {
            errEl = document.createElement('div');
            errEl.className = 'form-error';
            wrap.appendChild(errEl);
        }
        errEl.textContent = message;
    }

    function clearErrors() {
        document.querySelectorAll('.form-error').forEach(el => el.remove());
        document.querySelectorAll('.form-control.error').forEach(el => el.classList.remove('error'));
    }

    // ── Load from template JSON ─────────────────────────────────────────────
    function applyTemplate(valuesJson) {
        try {
            const vals = JSON.parse(valuesJson);
            Object.keys(vals).forEach(fieldId => {
                const input = getFieldInput(fieldId);
                if (!input) return;
                if (input.type === 'checkbox') input.checked = vals[fieldId] === 'true';
                else input.value = vals[fieldId];
                input.dispatchEvent(new Event('change'));
            });
        } catch (e) {}
    }

    // ── Smart KB suggest (fires on Summary field) ───────────────────────────
    function setupKbSuggest(summaryInputId, suggestContainerId, categoryId) {
        const summaryInput = document.getElementById(summaryInputId);
        const suggestContainer = document.getElementById(suggestContainerId);
        if (!summaryInput || !suggestContainer) return;

        let debounce;
        summaryInput.addEventListener('input', () => {
            clearTimeout(debounce);
            const q = summaryInput.value.trim();
            if (q.length < 3) { suggestContainer.innerHTML = ''; return; }
            debounce = setTimeout(() => {
                fetch(`/Handlers/FormFieldConfig.ashx?action=kbsuggest&q=${encodeURIComponent(q)}&cat=${categoryId || ''}`)
                    .then(r => r.json()).then(data => {
                        if (!data.articles || data.articles.length === 0) {
                            suggestContainer.innerHTML = '';
                            return;
                        }
                        suggestContainer.innerHTML = `
                            <div class="kb-suggest-header">
                                <span>💡 Related Knowledge Base Articles</span>
                            </div>` +
                            data.articles.map(a => `
                                <a href="/Pages/Shared/KnowledgeBase.aspx?id=${a.articleId}" target="_blank"
                                   class="kb-suggest-item">
                                  <strong>${escHtml(a.title)}</strong>
                                </a>`).join('');
                    }).catch(() => {});
            }, 400);
        });
    }

    function escHtml(str) {
        if (!str) return '';
        return String(str).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;');
    }

    return { init, validate, collectValues, applyTemplate, setupKbSuggest };
})();
