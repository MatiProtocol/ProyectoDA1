let cy = undefined;
cytoscape.use(cytoscapeKlay);
window.initCytoscape = function (vertex, edge) {
    if (cy = undefined) {
        cy.destroy();
        cy = undefined;
    }

    cy = window.cy = cytoscape({
        container: document.getElementById('Graph'),

        style: [
            {
                selector: 'node[name]',
                style: {
                    color: "#f5f5f5",
                    content: "data(name)",
                }
            },

            {
                selector: 'node[state="Pending"]',
                style: {
                    'background-color': '#d50000'
                }
            },
            {
                selector: 'node[state="Planed"]',
                style: {
                    'background-color': '#ffae00'
                }
            },
            {
                selector: 'node[state="Started"]',
                style: {
                    'background-color': '#f0fe1e'
                }
            },
            {
                selector: 'node[state="Finished"]',
                style: {
                    'background-color': '#2a9c01'
                }
            },

            {
                selector: 'edge[type="SS"]',
                style: {
                    'width': 4,
                    'target-arrow-shape': 'triangle',
                    'line-color': '#00ff00',
                    'target-arrow-color': '#00ff00',
                    'curve-style': 'bezier'
                }
            },
            {
                selector: 'edge[type="SS"][criticalPath="True"]',
                style: {
                    'width': 4,
                    'target-arrow-shape': 'triangle',
                    'line-color': '#748B00',
                    'target-arrow-color': '#748B00',
                    'curve-style': 'bezier'
                }
            },

            {
                selector: 'edge[type="FS"]',
                style: {
                    'width': 4,
                    'target-arrow-shape': 'triangle',
                    'line-color': '#0000ff',
                    'target-arrow-color': '#0000ff',
                    'curve-style': 'bezier'
                }
            },
            {
                selector: 'edge[type="FS"][criticalPath="True"]',
                style: {
                    'width': 4,
                    'target-arrow-shape': 'triangle',
                    'line-color': '#74008B',
                    'target-arrow-color': '#74008B',
                    'curve-style': 'bezier'
                }
            },
        ],
        elements: {
            nodes: vertex,
            edges: edge, 
        }
    });

    cy.layout({
        name: 'klay',
        fit: true,                  // Ensures the whole graph fits in the viewport
        padding: 20,                // Padding around the graph
        animate: false,             // Optional: disable animation for fast redraw

        klay: {
            direction: 'RIGHT',       // Layout direction: LEFT, RIGHT, UP, DOWN
            edgeSpacingFactor: 1.5,   // More space between nodes and edges
            inLayerSpacingFactor: 1.5,// Space between nodes on the same level
            nodePlacement: 'BRANDES_KOEPF', // Nicest node alignment
            spacing: 50,              // Base spacing between nodes
            layoutHierarchy: true     // Enforce clear levels
        }
    }).run();

    cy.nodeHtmlLabel([{
        query: 'node[slack]',
        halign: 'center',
        valign: 'center',
        halignBox: 'center',
        valignBox: 'center',
        tpl: function(data) {
            console.log(data);
            return `<div style="color: lightblue; font-size: 8px;">${data.slack}</div>`;
        }
    }]);
};
