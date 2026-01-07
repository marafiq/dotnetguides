import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export function Dashboard({ totalResidents, totalMedications, pendingTasks }) {
    return (_jsxs("div", { className: "dashboard", children: [_jsx("h1", { children: "Dashboard" }), _jsxs("div", { className: "stats", children: [_jsx(StatCard, { label: "Residents", value: totalResidents }), _jsx(StatCard, { label: "Medications", value: totalMedications }), _jsx(StatCard, { label: "Pending Tasks", value: pendingTasks })] })] }));
}
function StatCard({ label, value }) {
    return (_jsxs("div", { className: "stat-card", children: [_jsx("div", { className: "stat-value", children: value }), _jsx("div", { className: "stat-label", children: label })] }));
}
//# sourceMappingURL=Component.js.map