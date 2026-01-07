import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
export function ResidentDetail({ id, name, room, admitDate, allergies }) {
    return (_jsxs("div", { className: "resident-detail", children: [_jsx("a", { href: "/residents", children: "\u2190 Back" }), _jsx("h1", { children: name }), _jsxs("dl", { children: [_jsx("dt", { children: "Room" }), _jsx("dd", { children: room }), _jsx("dt", { children: "Admitted" }), _jsx("dd", { children: new Date(admitDate).toLocaleDateString() }), _jsx("dt", { children: "Allergies" }), _jsx("dd", { children: allergies.length > 0 ? allergies.join(', ') : 'None' })] }), _jsx("div", { id: "medications", "data-impulse-deferred": `/residents/${id}/medications`, children: "Loading medications..." })] }));
}
export function Medications({ medications }) {
    if (medications.length === 0) {
        return _jsx("p", { children: "No medications" });
    }
    return (_jsx("ul", { className: "medications", children: medications.map((m) => (_jsxs("li", { children: [_jsx("strong", { children: m.name }), " ", m.dosage, " - ", m.frequency] }, m.id))) }));
}
//# sourceMappingURL=Detail.js.map