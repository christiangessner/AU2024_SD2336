import { TabulatorFull as Tabulator } from "https://unpkg.com/tabulator-tables@6.2.1/dist/js/tabulator_esm.min.js";

document.getElementById("content").style.display = "none";
document.getElementById("loading").style.display = "block";

const params = new Proxy(new URLSearchParams(window.location.search), {
    get: (searchParams, prop) => searchParams.get(prop),
});
let project = params.project;
let item = params.item;
let debugMode = false;

//#region Test Data
if (!project || !item) {
    project = "a.YnVzaW5lc3M6Y29vbG9yYW5nZTE0IzIwMjIxMDIxNTcwMzEwMDA2"; // TODO: Replace with your own project ID
    item = "urn:adsk.wipprod:dm.lineage:XNxysPoGRIquUjcwYF50aw"; // TODO: Replace with your own item (lineage) URN
    //item = "urn:adsk.wipprod:dm.lineage:DH4FEMiZSOW8bTPDiK06Dw"; // big assembly test
    //item = "urn:adsk.wipprod:dm.lineage:SBWdapiNQDaIS8dHZFb2Qw"; // small test assembly
    debugMode = true;
}
//#endregion

//#region Tabulator
const thumbnailFormatter = function (cell, formatterParams, onRendered) {
    if (cell.getRow().getData().thumbnailUrl) {
        return (
            "<img class='thumbnailImage' width='22' height='22' src='" +
            cell.getRow().getData().thumbnailUrl +
            "'>"
        );
    } else {
        return "<img class='statusImage' width='16' height='16' src='/images/spinner.svg'>";
    }
};

const itemActionFormatter = function (cell, formatterParams, onRendered) {
    if (cell.getRow().getData().itemStatus) {
        return (
            "<img class='statusImage' width='16' height='16' src='/images/" +
            cell.getRow().getData().itemStatus +
            ".svg'>"
        );
    } else {
        return "<img class='statusImage' width='16' height='16' src='/images/unknown.svg'>";
    }
};

const bomActionFormatter = function (cell, formatterParams, onRendered) {
    if (
        !cell.getRow().getData().children ||
        cell.getRow().getData().children.length == 0
    ) {
        return "<img class='statusImage' width='16' height='16' src='/images/none.svg'>";
    } else if (cell.getRow().getData().bomStatus) {
        return (
            "<img class='statusImage' width='16' height='16' src='/images/" +
            cell.getRow().getData().bomStatus +
            ".svg'>"
        );
    } else {
        return "<img class='statusImage' width='16' height='16' src='/images/unknown.svg'>";
    }
};

const itemHeaderFormater = function (cell, formatterParams, onRendered) {
    return "<img class='statusImage' width='16' height='16' src='/images/item.svg'>";
};

const bomHeaderFormater = function (cell, formatterParams, onRendered) {
    return "<img class='statusImage' width='16' height='16' src='/images/bom.svg'>";
};

function createTabulatorTable(data) {
    let table = new Tabulator("#bom", {
        height: "auto",
        rowHeight: 28,
        layout: "fitDataFill", //"fitColumns",
        index: "absolutePosition",
        data: data,
        dataTree: true,
        dataTreeChildIndent: 18,
        dataTreeStartExpanded: true,
        dataTreeChildField: "children",
        columns: [
            { field: "itemStatus", visible: false, htmlOutput: false },
            { field: "itemTooltip", visible: false, htmlOutput: false },
            { field: "bomStatus", visible: false, htmlOutput: false },
            { field: "bomTooltip", visible: false, htmlOutput: false },
            { field: "thumbnailUrl", visible: false, htmlOutput: false },
            { title: "Part Number", field: "partNumber" },
            {
                formatter: thumbnailFormatter,
                hozAlign: "center",
                vertAlign: "middle",
                headerSort: false,
                width: 28,
                minWidth: 28,
                maxWidth: 28,
                tooltip: function (e, cell, onRendered) {
                    if (cell._cell) {
                        return (
                            "<img width='128' height='128' src='" +
                            cell.getRow().getData().thumbnailUrl +
                            "'>"
                        );
                    }
                },
            },
            { title: "Pos", field: "absolutePosition", width: 75 },
            { title: "Qty", field: "quantity", width: 45, headerSort: false },
            { title: "Name", field: "name" },
            { title: "Material", field: "materialName" },
            {
                title: "Weight (kg)",
                field: "weight",
                width: 95,
                headerSort: false,
                hozAlign: "right",
                headerHozAlign: "right",
            },
            {
                title: "Volume (ccm)",
                field: "volume",
                width: 110,
                headerSort: false,
                hozAlign: "right",
                headerHozAlign: "right",
            },
            {
                titleFormatter: itemHeaderFormater,
                formatter: itemActionFormatter,
                headerHozAlign: "center",
                hozAlign: "center",
                vertAlign: "middle",
                headerSort: false,
                width: 28,
                frozen: true,
                tooltip: function (e, cell, onRendered) {
                    if (cell._cell) {
                        return cell.getRow().getData().itemTooltip;
                    }
                },
            },
            {
                titleFormatter: bomHeaderFormater,
                formatter: bomActionFormatter,
                headerHozAlign: "center",
                hozAlign: "center",
                vertAlign: "middle",
                headerSort: false,
                width: 28,
                frozen: true,
                tooltip: function (e, cell, onRendered) {
                    if (cell._cell) {
                        return cell.getRow().getData().bomTooltip;
                    }
                },
            },
            { title: "ERP Number", field: "erpNumber", width: 125, frozen: true },
        ],
    });

    return table;
}
//#endregion

//#region Functions
async function getUpdatedRow(
    sourceRow,
    currentRow = undefined
) {
    if (!currentRow) {
        currentRow = table.getRow(1);
    }

    if (currentRow.getData().id == sourceRow.getData().id) {
        return currentRow;
    }

    let updatedRow = undefined;
    var children = currentRow.getTreeChildren();
    if (children && children.length > 0) {
        for (let i = 0; i < children.length; i++) {
            updatedRow = await getUpdatedRow(sourceRow, children[i]);
            if (updatedRow) {
                break;
            }
        };
    }
    return updatedRow;
}

async function syncIdenticalRows(
    sourceRow,
    updateObject,
    currentRow = undefined
) {
    if (!currentRow) {
        currentRow = table.getRow(1);
        await sourceRow.update(updateObject);
        await sourceRow.reformat();
    }

    var children = currentRow.getTreeChildren();
    if (children && children.length > 0) {
        await Promise.all(
            children.map(async (row) => {
                await syncIdenticalRows(sourceRow, updateObject, row);

                if (row.getData().id == sourceRow.getData().id && row != sourceRow) {
                    await row.update(updateObject);
                    await row.reformat();
                }
            })
        );
    }
}

// async function updateAllRows(updateObject, currentRow = undefined) {
//     if (!currentRow) {
//         currentRow = table.getRow(1);
//         await currentRow.update(updateObject);
//         await currentRow.reformat();
//     }

//     var children = currentRow.getTreeChildren();
//     if (children && children.length > 0) {
//         await Promise.all(
//             children.map(async (row) => {
//                 await updateAllRows(updateObject, row);

//                 await row.update(updateObject);
//                 await row.reformat();
//             })
//         );
//     }
// }

async function getApsUserProfile() {
    try {
        const response = await axios.get("/api/auth/profile");
        return response.data;
    } catch (error) {
        console.error(error);
    }
}

async function getDataModelHierarchy(project, item) {
    try {
        const response = await axios.get(
            `/api/datamodel/projects/${project}/items/${item}/hierarchy`
        );
        return response.data;
    } catch (error) {
        console.error(error);
        throw error;
    }
}

async function updateComponentVersionAsyncDatas(
    currentRow = undefined,
    updated = []
) {
    var isRoot = false;
    if (!currentRow) {
        currentRow = table.getRow(1);
        isRoot = true;
    }

    var children = currentRow.getTreeChildren();
    if (children && children.length > 0) {
        await Promise.all(
            children.map(async (row) => {
                await updateComponentVersionAsyncDatas(row, updated);

                if (!updated.includes(row.getData().id)) {
                    updated.push(row.getData().id);
                    await updateComponentVersionAsyncData(row);
                }
            })
        );
    }

    if (isRoot) {
        await updateComponentVersionAsyncData(currentRow);
    }
}

async function updateComponentVersionAsyncData(row) {
    try {
        const response = await axios.get(
            `/api/datamodel/componentVersions/${row.getData().id}/async`
        );
        await syncIdenticalRows(row, {
            thumbnailUrl: response.data.thumbnailUrl,
            weight: response.data.weight,
            volume: response.data.volume,
        });
    } catch (error) {
        console.error(error);
        await syncIdenticalRows(row, {
            thumbnailUrl: "/images/unknown.svg",
            weight: -1,
            volume: -1,
        });
    }
}

async function updateErpStates(currentRow = undefined, updated = []) {
    var isRoot = false;
    if (!currentRow) {
        currentRow = table.getRow(1);
        isRoot = true;
    }

    var children = currentRow.getTreeChildren();
    if (children && children.length > 0) {
        await Promise.all(
            children.map(async (row) => {
                await updateErpStates(row, updated);

                if (!updated.includes(row.getData().id)) {
                    updated.push(row.getData().id);
                    await updateErpState(row);
                }
            })
        );
    }

    if (isRoot) {
        await updateErpState(currentRow, updated);
    }
}

async function updateErpState(row) {
    if (!row.getData().erpNumber) {
        let itemStatus = "add";
        let itemTooltip = "Item is being created in Business Central!";
        let bomStatus = "none";
        let bomTooltip = undefined;
        if (row.getData().children && row.getData().children.length > 0) {
            bomStatus = "add";
            bomTooltip = "BOM is being created in Business Central!";
        }
        await syncIdenticalRows(row, {
            itemStatus: itemStatus,
            itemTooltip: itemTooltip,
            bomStatus: bomStatus,
            bomTooltip: bomTooltip,
        });
    } else {
        let itemCompareResult = await getErpItemState(row);
        let itemStatus = itemCompareResult.status;
        let itemTooltip = itemCompareResult.message;
        let bomStatus = "none";
        let bomTooltip = undefined;
        if (row.getData().children && row.getData().children.length > 0) {
            let bomCompareResult = await getErpBomState(row);
            bomStatus = bomCompareResult.status;
            bomTooltip = bomCompareResult.message;
        }
        await syncIdenticalRows(row, {
            itemStatus: itemStatus,
            itemTooltip: itemTooltip,
            bomStatus: bomStatus,
            bomTooltip: bomTooltip,
        });
    }
}

async function getErpItemState(row) {
    try {
        const response = await axios.post(
            `/api/erp/items/${row.getData().erpNumber}/compare`,
            row.getData()
        );
        return response.data;
    } catch (error) {
        console.error(error);
    }
}

async function getErpBomState(row) {
    try {
        const response = await axios.post(
            `/api/erp/boms/${row.getData().erpNumber}/compare`,
            row.getData()
        );
        return response.data;
    } catch (error) {
        console.error(error);
    }
}

async function transferErpItemsAndBoms(currentRow = undefined, updated = []) {
    if (!currentRow) {
        currentRow = table.getRow(1);
    }

    if (!updated.includes(currentRow.getData().id)) {
        updated.push(currentRow.getData().id);
        await transferErpItem(currentRow);

        var children = currentRow.getTreeChildren();
        if (children && children.length > 0) {
            await Promise.all(
                children.map(async (row) => {
                    await transferErpItemsAndBoms(row, updated);
                })
            );
            await transferErpBom(currentRow);
        }
    }
}

async function transferErpItem(row) {
    try {
        const status = row.getData().itemStatus;
        const tooltip = row.getData().itemTooltip;
        await syncIdenticalRows(row, {
            itemStatus: "spinner",
            itemTooltip: "process...",
        });
        if (status == "add") {
            const response = await axios.post("/api/erp/items", row.getData());
            await setProperty(
                row.getData().id,
                row.getData().erpNumberDefinitionId,
                response.data.no
            );
            await syncIdenticalRows(row, {
                itemStatus: "identical",
                itemTooltip: "Item '" + response.data.no + "' created!",
                erpNumber: response.data.no,
            });
        } else if (status == "update") {
            const response = await axios.put(
                `/api/erp/items/${row.getData().erpNumber}`,
                row.getData()
            );
            await syncIdenticalRows(row, {
                itemStatus: "identical",
                itemTooltip: "Item '" + response.data.no + "' updated!",
            });
        } else {
            await syncIdenticalRows(row, {
                itemStatus: status,
                itemTooltip: tooltip,
            });
        }
    } catch (error) {
        console.error(error);
        await syncIdenticalRows(row, {
            itemStatus: "error",
            itemTooltip: error.message,
        });
    }
}

async function transferErpBom(row) {
    try {
        row = await getUpdatedRow(row);

        const status = row.getData().bomStatus;
        const tooltip = row.getData().bomTooltip;
        await syncIdenticalRows(row, { bomStatus: "spinner", bomTooltip: "process..." });
        if (status == "add") {
            const response = await axios.post("/api/erp/boms", row.getData());
            await syncIdenticalRows(row, {
                bomStatus: "identical",
                bomTooltip: "BOM '" + response.data.no + "' created!",
            });
        } else if (status == "update") {
            const response = await axios.put(
                `/api/erp/boms/${row.getData().erpNumber}`,
                row.getData()
            );
            await syncIdenticalRows(row, {
                bomStatus: "identical",
                bomTooltip: "BOM '" + response.data.no + "' updated!",
            });
        } else {
            await syncIdenticalRows(row, { bomStatus: status, bomTooltip: tooltip });
        }
    } catch (error) {
        console.error(error);
        await syncIdenticalRows(row, {
            bomStatus: "error",
            bomTooltip: error.message,
        });
    }
}

async function setProperty(componentVersionId, definitionId, value) {
    try {
        const response = await axios.post(
            `/api/datamodel/componentVersions/${componentVersionId}/properties`,
            {
                definitionId: definitionId,
                value: value,
            }
        );
        return response.data;
    } catch (error) {
        console.error(error);
    }
}

async function cleanItems(currentRow = undefined, updated = []) {
    if (!currentRow) {
        currentRow = table.getRow(1);
    }

    if (!updated.includes(currentRow.getData().id)) {
        updated.push(currentRow.getData().id);
        await cleanItem(currentRow);

        var children = currentRow.getTreeChildren();
        if (children && children.length > 0) {
            await Promise.all(
                children.map(async (row) => {
                    await cleanItems(row, updated);
                })
            );
        }
    }
}

async function cleanItem(row) {
    try {
        const number = row.getData().erpNumber;
        if (number) {
            await syncIdenticalRows(row, {
                itemStatus: "spinner",
                itemTooltip: "process...",
                bomStatus: "spinner",
                bomTooltip: "process..."
            });
            await cleanProperty(
                row.getData().id,
                row.getData().erpNumberDefinitionId,
            );
            await syncIdenticalRows(row, {
                itemStatus: "unknown",
                itemTooltip: "",
                bomStatus: "unknown",
                bomTooltip: "",
                erpNumber: ""
            });
        }
    } catch (error) {
        console.error(error);
        await syncIdenticalRows(row, {
            itemStatus: "error",
            itemTooltip: error.message,
            bomStatus: "unknown",
            bomTooltip: ""
        });
    }
}

async function cleanProperty(componentVersionId, definitionId) {
    try {
        const response = await axios.delete(
            `/api/datamodel/componentVersions/${componentVersionId}/properties/${definitionId}`
        );
        return response.data;
    } catch (error) {
        console.error(error);
    }
}
//#endregion

let table;

try {
    const loadingText = document.getElementById("loadingText");
    loadingText.innerText = "Authenticating (APS)...";
    const user = await getApsUserProfile();
    if (!user) {
        window.location.replace(`/api/auth/login/project/${project}/item/${item}`);
    } else {
        loadingText.innerText = "Loading Manufacturing Data Model...";
        const resetButton = document.getElementById("reset");
        const checkButton = document.getElementById("check");
        const transferButton = document.getElementById("transfer");

        if (debugMode) {
            resetButton.style.display = "block";
        } else {
            resetButton.style.display = "none";
        }

        checkButton.disabled = true;
        transferButton.disabled = true;

        const dataSource = await getDataModelHierarchy(project, item);
        console.log(dataSource);

        table = createTabulatorTable(dataSource);
        table.on("tableBuilt", function () {
            updateComponentVersionAsyncDatas().then(() => {
                checkButton.disabled = false;
            });
        });

        resetButton.addEventListener("click", async () => {
            checkButton.disabled = transferButton.disabled = true;
            await cleanItems();
            checkButton.disabled = transferButton.disabled = false;
        });

        checkButton.addEventListener("click", async () => {
            checkButton.disabled = transferButton.disabled = true;
            await updateErpStates();
            checkButton.disabled = transferButton.disabled = false;
        });

        transferButton.addEventListener("click", async () => {
            checkButton.disabled = transferButton.disabled = true;
            await transferErpItemsAndBoms();
            checkButton.disabled = transferButton.disabled = false;
        });

        document.getElementById("content").style.display = "block";
        document.getElementById("loading").style.display = "none";
    }
} catch (err) {
    alert("Could not initialize the application. See console for more details.");
    console.error(err);
}
