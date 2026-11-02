// Fluent UI 5.0.1 sends the text of a combobox's preset value only once, right after the first render, while the web
// component creates its text field asynchronously; the text is lost and the field looks empty although the value is
// chosen. This shows the text once the field exists (EntryForm, edit and copy windows).
export function showSelectedText(dropdown, text) {
    const show = (attempt) => {
        const control = dropdown?.control;
        if (control) {
            control.value = text;
        } else if (attempt < 30) {
            requestAnimationFrame(() => show(attempt + 1));
        }
    };
    show(0);
}
