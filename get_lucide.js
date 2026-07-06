const lucide = require('lucide');

const icons = ['mic', 'keyboard', 'bar-chart-2', 'settings'];

icons.forEach(name => {
    const icon = lucide.icons[name];
    if (icon) {
        console.log(`\n--- ${name} ---`);
        console.log(icon[2].map(node => {
            if (node[0] === 'path') return `<path d="${node[1].d}"/>`;
            if (node[0] === 'rect') {
                const attrs = Object.entries(node[1]).map(([k,v]) => `${k}="${v}"`).join(' ');
                return `<rect ${attrs}/>`;
            }
            if (node[0] === 'line') {
                const attrs = Object.entries(node[1]).map(([k,v]) => `${k}="${v}"`).join(' ');
                return `<line ${attrs}/>`;
            }
            if (node[0] === 'circle') {
                const attrs = Object.entries(node[1]).map(([k,v]) => `${k}="${v}"`).join(' ');
                return `<circle ${attrs}/>`;
            }
            return node;
        }).join('\n'));
    }
});
