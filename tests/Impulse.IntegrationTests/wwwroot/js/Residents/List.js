import { jsxs as _jsxs, jsx as _jsx } from "react/jsx-runtime";
export function ResidentsList({ residents, totalCount }) {
    return (_jsxs("div", { className: "residents-list", children: [_jsxs("h1", { children: ["Residents (", totalCount, ")"] }), _jsxs("table", { children: [_jsx("thead", { children: _jsxs("tr", { children: [_jsx("th", { children: "Name" }), _jsx("th", { children: "Room" })] }) }), _jsx("tbody", { children: residents.map((r) => (_jsxs("tr", { children: [_jsx("td", { children: _jsx("a", { href: `/residents/${r.id}`, children: r.name }) }), _jsx("td", { children: r.room })] }, r.id))) })] })] }));
}
//# sourceMappingURL=List.js.map