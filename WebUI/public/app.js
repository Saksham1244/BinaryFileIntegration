// app.js - Machine Monitoring Dashboard Logic

const machineNamesMap = {
    "LIL1600ML-054": "Shibaura 650 T1",
    "LIL1600ML-079": "Shibaura 1000T-2",
    "LIL1600ML-001": "JSW 1300T-1",
    "LIL1300ML-015": "CLF 190T",
    "LIL1600ML-011": "L&T 180T",
    "LIL1600ML-010": "Shibaura 500T-1",
    "LIL1600ML-065": "Shibaura 650 T3",
    "LIL1600ML-013": "Shibaura 100T",
    "LIL1600ML-063": "JSW 1300T-2",
    "LIL1300ML-033": "FCS 350 T3",
    "LIL1600ML-007": "BMC 550T",
    "LIL1600ML-076": "BMC 650T",
    "LIL1300ML-011": "CLF 400T",
    "LIL1600ML-043": "Shibaura 350T-2",
    "LIL1600ML-014": "Shibaura 150T",
    "LIL1600ML-046": "Shibaura 650T-2",
    "LIL1600ML-004": "Shibaura 350T-1",
    "LIL1600ML-039": "Shibaura 1000T-1",
    "LIL1600ML-002": "Shibaura 1075T"
};

let machinesData = [];
let currentFilter = 'all';
let searchQuery = '';
let countdownInterval = null;
let refreshSeconds = 10;
let currentCountdown = refreshSeconds;

// DOM Elements
const dbStatus = document.getElementById('dbStatus');
const countdownEl = document.getElementById('countdown');
const btnRefresh = document.getElementById('btnRefresh');
const lastUpdatedEl = document.getElementById('lastUpdated');
const machinesGrid = document.getElementById('machinesGrid');
const txtSearch = document.getElementById('txtSearch');
const filterTabs = document.querySelectorAll('.filter-tab');

// Stat values
const valTotal = document.getElementById('valTotal');
const valOnline = document.getElementById('valOnline');
const valOffline = document.getElementById('valOffline');
const valActive = document.getElementById('valActive');

// Format elapsed time nicely
function timeSince(dateString) {
    if (!dateString) return { text: 'No Data Received', class: 'dead', hours: 999 };
    
    // Convert SQL format (YYYY-MM-DD HH:MM:SS) to ISO format without timezone indicator to force local timezone parsing
    const date = new Date(dateString.replace(' ', 'T'));
    const now = new Date();
    const diffMs = now - date;
    const seconds = Math.floor(diffMs / 1000);
    const hours = diffMs / (1000 * 60 * 60);
    
    if (seconds < 5) return { text: 'Just now', class: 'recent', hours };
    
    const mins = Math.floor(seconds / 60);
    if (mins < 60) {
        return { text: `${mins} min${mins > 1 ? 's' : ''} ago`, class: 'recent', hours };
    }
    
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) {
        return { text: `${hrs} hr${hrs > 1 ? 's' : ''} ago`, class: 'stale', hours };
    }
    
    const days = Math.floor(hrs / 24);
    return { text: `${days} day${days > 1 ? 's' : ''} ago`, class: 'dead', hours };
}

// Fetch data from API
async function fetchStatus() {
    try {
        btnRefresh.classList.add('fa-spin');
        const response = await fetch('/api/status');
        const data = await response.json();
        
        if (data.success) {
            machinesData = data.machines;
            // Sort by friendly name
            machinesData.sort((a, b) => {
                const nameA = machineNamesMap[a.id] || a.id;
                const nameB = machineNamesMap[b.id] || b.id;
                return nameA.localeCompare(nameB);
            });
            updateDashboardHeader(data);
            updateSummaryStats();
            renderGrid();
        } else {
            console.error('API Error:', data.error);
            showErrorMessage(data.error);
        }
    } catch (err) {
        console.error('Fetch error:', err);
        showErrorMessage('Failed to connect to dashboard server.');
    } finally {
        setTimeout(() => {
            btnRefresh.classList.remove('fa-spin');
        }, 600);
    }
}

// Update Database status dot and timer text
function updateDashboardHeader(data) {
    if (data.dbConnected) {
        dbStatus.className = 'status-indicator db-status connected';
        dbStatus.querySelector('.status-label').textContent = 'DB: OK';
    } else {
        dbStatus.className = 'status-indicator db-status';
        dbStatus.querySelector('.status-label').textContent = 'DB: OFFLINE';
    }
    
    const now = new Date();
    lastUpdatedEl.textContent = now.toLocaleTimeString();
}

// Calculate summary stats
function updateSummaryStats() {
    const total = machinesData.length;
    const online = machinesData.filter(m => m.isOnline).length;
    const offline = total - online;
    
    // Count machines that pushed data in the last 24h
    let active24h = 0;
    machinesData.forEach(m => {
        if (m.lastData && m.lastData.process && m.lastData.process.TimeStamp) {
            const lastUpdate = new Date(m.lastData.process.TimeStamp.replace(' ', 'T'));
            const hoursSince = (new Date() - lastUpdate) / (1000 * 60 * 60);
            if (hoursSince <= 24) {
                active24h++;
            }
        }
    });

    valTotal.textContent = total;
    valOnline.textContent = online;
    valOffline.textContent = offline;
    valActive.textContent = active24h;
}

// Filter and render the cards
function renderGrid() {
    machinesGrid.innerHTML = '';
    
    const filtered = machinesData.filter(machine => {
        // Search Filter
        const friendlyName = machineNamesMap[machine.id] || '';
        const matchesSearch = machine.id.toLowerCase().includes(searchQuery.toLowerCase()) || 
                             friendlyName.toLowerCase().includes(searchQuery.toLowerCase()) ||
                             (machine.ip && machine.ip.includes(searchQuery));
        
        if (!matchesSearch) return false;
        
        // Tab Filter
        if (currentFilter === 'all') return true;
        if (currentFilter === 'online') return machine.isOnline;
        if (currentFilter === 'offline') return !machine.isOnline;
        
        if (currentFilter === 'stale') {
            if (!machine.lastData || !machine.lastData.process) return true; // never got data is stale
            const timeInfo = timeSince(machine.lastData.process.TimeStamp);
            return timeInfo.hours > 1;
        }
        
        return true;
    });

    if (filtered.length === 0) {
        machinesGrid.innerHTML = `
            <div class="glass-panel" style="grid-column: 1/-1; padding: 3rem; text-align: center; color: var(--text-secondary);">
                <i class="fa-solid fa-circle-info" style="font-size: 2.5rem; margin-bottom: 1rem; color: var(--color-cyan);"></i>
                <p>No machines match the selected filter or search query.</p>
            </div>
        `;
        return;
    }

    filtered.forEach(m => {
        const card = document.createElement('div');
        card.className = `machine-card ${m.isOnline ? 'online' : 'offline'}`;
        
        // Extract process details
        const hasProcess = m.lastData && m.lastData.process;
        const lastShot = hasProcess ? parseFloat(m.lastData.process.Shot_Count).toLocaleString() : 'N/A';
        const cycleTime = hasProcess ? `${parseFloat(m.lastData.process.Cycle_Time).toFixed(1)}s` : 'N/A';
        const shift = hasProcess ? m.lastData.process.ShiftName : 'N/A';
        
        // Extract machine info
        const hasMachine = m.lastData && m.lastData.machine;
        const mouldId = hasMachine ? m.lastData.machine.Mould_ID : 'N/A';
        const partNumber = hasMachine ? m.lastData.machine.Part_Number : 'N/A';
        const totalShots = hasMachine ? parseFloat(m.lastData.machine.Total_Shots).toLocaleString() : 'N/A';
        
        // Calculate age of data based on TimeStamp (DB insert time)
        const updateTimeInfo = hasProcess ? timeSince(m.lastData.process.TimeStamp) : { text: 'No Data', class: 'dead' };
        
        const hasAlarm = m.lastData && m.lastData.alarms && m.lastData.alarms.length > 0 && m.lastData.alarms[0].Reset_Date_Time === 'Active';
        const alarms = (m.lastData && m.lastData.alarms) ? m.lastData.alarms : [];
        
        const offlineTime = m.lastOfflineTime ? m.lastOfflineTime : 'N/A';
        const onlineTime = m.lastOnlineTime ? m.lastOnlineTime : 'N/A';
        
        const commStatusHtml = m.isOnline 
            ? `<div style="text-align: right; display: flex; flex-direction: column; align-items: flex-end; gap: 0.25rem;">
                <span class="ping-badge"><i class="fa-solid fa-circle-check"></i> Communicating</span>
                ${m.lastOnlineTime ? `<span style="font-size: 0.7rem; color: var(--text-secondary);"><i class="fa-solid fa-plug-circle-check"></i> Recovered: ${onlineTime}</span>` : ''}
               </div>`
            : `<div style="text-align: right; display: flex; flex-direction: column; align-items: flex-end; gap: 0.25rem;">
                <span class="ping-badge"><i class="fa-solid fa-circle-xmark"></i> No Connection</span>
                ${m.lastOfflineTime ? `<span style="font-size: 0.7rem; color: var(--text-secondary);"><i class="fa-solid fa-plug-circle-xmark"></i> Stopped: ${offlineTime}</span>` : ''}
               </div>`;
        
        card.innerHTML = `
            <div class="card-header">
                <div class="card-title">
                    <h2>${machineNamesMap[m.id] || m.id}</h2>
                    <span>ID: ${m.id} | IP: ${m.ip}</span>
                </div>
                ${commStatusHtml}
            </div>
            
            <div class="time-badge ${updateTimeInfo.class}">
                <i class="fa-solid fa-clock-rotate-left"></i>
                <span>Last Data: ${updateTimeInfo.text}</span>
            </div>
            
            <div class="card-stats-grid">
                <div class="card-stat">
                    <span class="label">Shot Count</span>
                    <span class="value" style="color: var(--color-cyan);">${lastShot}</span>
                </div>
                <div class="card-stat">
                    <span class="label">Cycle Time</span>
                    <span class="value">${cycleTime}</span>
                </div>
                <div class="card-stat">
                    <span class="label">Mould ID</span>
                    <span class="value" title="${mouldId}">${mouldId}</span>
                </div>
                <div class="card-stat">
                    <span class="label">Shift</span>
                    <span class="value">${shift}</span>
                </div>
            </div>
            
            <div class="card-details">
                <div class="detail-row">
                    <span>Part Number:</span>
                    <span class="detail-val" title="${partNumber}">${partNumber}</span>
                </div>
                <div class="detail-row">
                    <span>Total Shots (MAC):</span>
                    <span class="detail-val">${totalShots}</span>
                </div>
                <div class="detail-row">
                    <span>Machine Update Time:</span>
                    <span class="detail-val">${hasProcess ? m.lastData.process.Date_Time : 'N/A'}</span>
                </div>
            </div>
            
            ${alarms.length > 0 ? `
                <div class="alarm-history">
                    <div class="alarm-history-header">
                        <i class="fa-solid fa-bell"></i>
                        <span>Recent Alarms</span>
                        ${hasAlarm ? '<span class="alarm-active-badge"><i class="fa-solid fa-circle" style="font-size:0.5rem;"></i> ACTIVE</span>' : ''}
                    </div>
                    <table class="alarm-table">
                        <thead>
                            <tr>
                                <th>#</th>
                                <th>Description</th>
                                <th>Set Time</th>
                                <th>Reset Time</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${alarms.map(a => {
                                const isActive = a.Reset_Date_Time === 'Active';
                                return `<tr class="${isActive ? 'alarm-row-active' : 'alarm-row-cleared'}">
                                    <td>${a.Alarm_Number}</td>
                                    <td title="${a.Alarm_Status}">${a.Alarm_Status}</td>
                                    <td>${a.Set_Date_Time}</td>
                                    <td><span class="alarm-status-tag ${isActive ? 'tag-active' : 'tag-cleared'}">${isActive ? 'ACTIVE' : a.Reset_Date_Time}</span></td>
                                </tr>`;
                            }).join('')}
                        </tbody>
                    </table>
                </div>
            ` : ''}
        `;
        
        machinesGrid.appendChild(card);
    });
}

// DOM Elements for Navigation Tabs
const navDashboardBtn = document.getElementById('navDashboardBtn');
const navParametersBtn = document.getElementById('navParametersBtn');
const dashboardView = document.getElementById('dashboardView');
const parametersView = document.getElementById('parametersView');

// Parameter View selectors
const paramDate = document.getElementById('paramDate');
const paramShift = document.getElementById('paramShift');
const paramStartTime = document.getElementById('paramStartTime');
const paramEndTime = document.getElementById('paramEndTime');
const machineCardsSlider = document.getElementById('machineCardsSlider');
const sliderPrevBtn = document.getElementById('sliderPrevBtn');
const sliderNextBtn = document.getElementById('sliderNextBtn');
const btnLoadTrends = document.getElementById('btnLoadTrends');
const btnExportCSV = document.getElementById('btnExportCSV');
const chartContainers = {
    Pressure: document.getElementById('chartPressure'),
    Speed: document.getElementById('chartSpeed'),
    Temperature: document.getElementById('chartTemperature'),
    Timing: document.getElementById('chartTiming')
};
const tableCard = document.getElementById('tableCard');
const trendTableHeader = document.getElementById('trendTableHeader');
const trendTableBody = document.getElementById('trendTableBody');
const tablePagination = document.getElementById('tablePagination');

// Selected states
let selectedMachineId = '';
let activeCharts = {
    Pressure: null,
    Speed: null,
    Temperature: null,
    Timing: null
};
let trendData = [];
let currentPage = 1;
const rowsPerPage = 10;

// Parameter Config lists
const mouldingParams = {
    "Pressure": [
        { id: "Holding_pressure_step_1", label: "Holding Press Step 1" },
        { id: "Holding_pressure_step_2", label: "Holding Press Step 2" },
        { id: "Holding_pressure_step_3", label: "Holding Press Step 3" },
        { id: "Holding_pressure_step_4", label: "Holding Press Step 4" },
        { id: "Injection_pressure_step_1", label: "Inj Press Step 1" },
        { id: "Injection_pressure_step_2", label: "Inj Press Step 2" },
        { id: "Injection_pressure_step_3", label: "Inj Press Step 3" },
        { id: "Injection_pressure_step_4", label: "Inj Press Step 4" },
        { id: "Dosing_back_pressure_step_1", label: "Dosing Back Press 1" },
        { id: "Dosing_back_pressure_step_2", label: "Dosing Back Press 2" },
        { id: "Dosing_back_pressure_step_3", label: "Dosing Back Press 3" }
    ],
    "Speed": [
        { id: "Injection_speed_step_1", label: "Inj Speed Step 1" },
        { id: "Injection_speed_step_2", label: "Inj Speed Step 2" },
        { id: "Injection_speed_step_3", label: "Inj Speed Step 3" },
        { id: "Injection_speed_step_4", label: "Inj Speed Step 4" },
        { id: "Dosing_speed_step_1", label: "Dosing Speed Step 1" },
        { id: "Dosing_speed_step_2", label: "Dosing Speed Step 2" },
        { id: "Dosing_speed_step_3", label: "Dosing Speed Step 3" },
        { id: "Dosing_speed_actual", label: "Dosing Speed Actual" }
    ],
    "Temperature": [
        { id: "Barrel_temperature_actual_nozzle", label: "Barrel Temp Nozzle" },
        { id: "Barrel_temperature_actual_zone_1", label: "Barrel Temp Zone 1" },
        { id: "Barrel_temperature_actual_zone_2", label: "Barrel Temp Zone 2" },
        { id: "Barrel_temperature_actual_zone_3", label: "Barrel Temp Zone 3" },
        { id: "Barrel_temperature_actual_zone_4", label: "Barrel Temp Zone 4" },
        { id: "Barrel_temperature_actual_zone_5", label: "Barrel Temp Zone 5" },
        { id: "Oil_temperature_actual", label: "Oil Temp Actual" },
        { id: "Hot_runner_temperature_actual_zone_1", label: "Hot Runner Zone 1" },
        { id: "Hot_runner_temperature_actual_zone_2", label: "Hot Runner Zone 2" },
        { id: "Hot_runner_temperature_actual_zone_3", label: "Hot Runner Zone 3" },
        { id: "Hot_runner_temperature_actual_zone_4", label: "Hot Runner Zone 4" },
        { id: "Hot_runner_temperature_actual_zone_5", label: "Hot Runner Zone 5" },
        { id: "Hot_runner_temperature_actual_zone_6", label: "Hot Runner Zone 6" },
        { id: "Hot_runner_temperature_actual_zone_7", label: "Hot Runner Zone 7" },
        { id: "Hot_runner_temperature_actual_zone_8", label: "Hot Runner Zone 8" },
        { id: "Hot_runner_temperature_actual_zone_9", label: "Hot Runner Zone 9" },
        { id: "Hot_runner_temperature_actual_zone_10", label: "Hot Runner Zone 10" },
        { id: "Hot_runner_temperature_actual_zone_11", label: "Hot Runner Zone 11" },
        { id: "Hot_runner_temperature_actual_zone_12", label: "Hot Runner Zone 12" }
    ],
    "Timing": [
        { id: "Holding_time_step_1", label: "Holding Time Step 1" },
        { id: "Holding_time_step_2", label: "Holding Time Step 2" },
        { id: "Holding_time_step_3", label: "Holding Time Step 3" },
        { id: "Holding_time_step_4", label: "Holding Time Step 4" },
        { id: "Injection_time_actual", label: "Inj Time Actual" },
        { id: "Cooling_time_actual", label: "Cooling Time Actual" },
        { id: "Dosing_time_actual", label: "Dosing Time Actual" },
        { id: "Cascade_injection_delay_time_1", label: "Cascade Delay 1" },
        { id: "Cascade_injection_delay_time_2", label: "Cascade Delay 2" },
        { id: "Cascade_injection_delay_time_3", label: "Cascade Delay 3" },
        { id: "Cascade_injection_delay_time_4", label: "Cascade Delay 4" },
        { id: "Cascade_injection_delay_time_5", label: "Cascade Delay 5" },
        { id: "Cascade_injection_delay_time_6", label: "Cascade Delay 6" },
        { id: "Cascade_injection_delay_time_7", label: "Cascade Delay 7" },
        { id: "Cascade_injection_delay_time_8", label: "Cascade Delay 8" }
    ]
};

const processParams = {
    "Pressure": [
        { id: "Switch_Over_Pressure", label: "Switch Over Press" },
        { id: "Peak_Injection_Pressure", label: "Peak Inj Press" }
    ],
    "Speed": [],
    "Temperature": [
        { id: "Zone_1_Temeprature", label: "Zone 1 Temp" },
        { id: "Zone_2_Temeprature", label: "Zone 2 Temp" },
        { id: "Zone_3_Temperature", label: "Zone 3 Temp" },
        { id: "Zone_4_Temeprature", label: "Zone 4 Temp" },
        { id: "Nozzle_1_Temeprature", label: "Nozzle 1 Temp" },
        { id: "Nozzle_2_Temperaturee", label: "Nozzle 2 Temp" },
        { id: "Feed_Temperature", label: "Feed Temp" },
        { id: "Melt_Temperature", label: "Melt Temp" },
        { id: "Oil_Temperature", label: "Oil Temp" },
        { id: "Mold_Zone1_Temperature", label: "Mold Zone 1 Temp" },
        { id: "Mold_Zone2_Temperature", label: "Mold Zone 2 Temp" },
        { id: "MTC_Temperature", label: "MTC Temp" }
    ],
    "Timing": [
        { id: "Cycle_Time", label: "Cycle Time" },
        { id: "Injection_Time", label: "Injection Time" },
        { id: "Dosing_Time", label: "Dosing Time" },
        { id: "Mold_Close_Time", label: "Mold Close Time" },
        { id: "Mold_Open_Time", label: "Mold Open Time" },
        { id: "Tonnage_Build_Time", label: "Tonnage Build Time" },
        { id: "Tonnage_Release_Time", label: "Tonnage Release Time" },
        { id: "Ejector_Forward_Time", label: "Ejector Fwd Time" },
        { id: "Ejector_Back_Time", label: "Ejector Back Time" }
    ]
};

// Render switches dynamically
function renderParameterSwitches() {
    const categories = ["Pressure", "Speed", "Temperature", "Timing"];
    
    categories.forEach(cat => {
        const grid = document.getElementById(`grid${cat}`);
        grid.innerHTML = '';
        
        const mouldingList = mouldingParams[cat] || [];
        const processList = processParams[cat] || [];
        const combined = [...mouldingList, ...processList];
        
        combined.forEach(p => {
            const container = document.createElement('label');
            container.className = 'switch-container';
            container.dataset.paramId = p.id;
            
            container.innerHTML = `
                <input type="checkbox" value="${p.id}" class="param-checkbox" data-cat="${cat}">
                <div class="switch-track">
                    <div class="switch-thumb"></div>
                </div>
                <span>${p.label}</span>
            `;
            
            const checkbox = container.querySelector('input');
            checkbox.addEventListener('change', () => {
                if (checkbox.checked) {
                    container.classList.add('checked');
                } else {
                    container.classList.remove('checked');
                }
                
                // Automatically refresh trends in real-time
                const checkedCount = document.querySelectorAll('.param-checkbox:checked').length;
                if (checkedCount > 0) {
                    loadTrendsData();
                } else {
                    // Hide visualization panels if no parameters are selected
                    Object.values(chartContainers).forEach(c => c.style.display = 'none');
                    tableCard.style.display = 'none';
                    btnExportCSV.disabled = true;
                    Object.keys(activeCharts).forEach(cat => {
                        if (activeCharts[cat]) {
                            activeCharts[cat].destroy();
                            activeCharts[cat] = null;
                        }
                    });
                }
            });
            
            grid.appendChild(container);
        });
    });
}

// Render horizontal machines list slider
function renderMachineSlider() {
    machineCardsSlider.innerHTML = '';
    
    machinesData.forEach(m => {
        const friendlyName = machineNamesMap[m.id] || m.id;
        const card = document.createElement('div');
        card.className = `slider-machine-card ${selectedMachineId === m.id ? 'selected' : ''}`;
        
        card.innerHTML = `
            <div class="slider-machine-status ${m.isOnline ? 'bg-success' : 'bg-error'}"></div>
            <h5>${friendlyName}</h5>
            <span>${m.id}</span>
        `;
        
        card.addEventListener('click', () => {
            document.querySelectorAll('.slider-machine-card').forEach(c => c.classList.remove('selected'));
            card.classList.add('selected');
            selectedMachineId = m.id;
            
            // Automatically refresh trends if any parameter checkboxes are active
            const checkedCount = document.querySelectorAll('.param-checkbox:checked').length;
            if (checkedCount > 0) {
                loadTrendsData();
            }
        });
        
        machineCardsSlider.appendChild(card);
    });
    
    // Auto-select first machine if none selected
    if (!selectedMachineId && machinesData.length > 0) {
        selectedMachineId = machinesData[0].id;
        const firstCard = machineCardsSlider.querySelector('.slider-machine-card');
        if (firstCard) firstCard.classList.add('selected');
    }
}

// Client-side merging algorithm for Moulding and Process data on TimeStamp
function mergeTimeSeries(moulding, process) {
    const timeMap = {};
    
    process.forEach(row => {
        const timeKey = row.TimeStamp;
        if (!timeMap[timeKey]) timeMap[timeKey] = {};
        Object.assign(timeMap[timeKey], row);
    });
    
    moulding.forEach(row => {
        const timeKey = row.TimeStamp;
        // Match records written within 3s of each other to align same shot
        const matchingKey = Object.keys(timeMap).find(k => {
            return Math.abs(new Date(k) - new Date(timeKey)) <= 3000;
        });
        
        if (matchingKey) {
            Object.assign(timeMap[matchingKey], row);
        } else {
            timeMap[timeKey] = row;
        }
    });
    
    return Object.keys(timeMap)
        .map(k => {
            const item = timeMap[k];
            item.TimeStamp = k;
            return item;
        })
        .sort((a, b) => new Date(a.TimeStamp) - new Date(b.TimeStamp));
}

// Generate the trends visualization chart and table
async function loadTrendsData() {
    if (!selectedMachineId) {
        alert('Please select a machine first.');
        return;
    }
    
    const checkboxes = document.querySelectorAll('.param-checkbox:checked');
    if (checkboxes.length === 0) {
        alert('Please select at least one parameter to view trends.');
        return;
    }
    
    const fields = Array.from(checkboxes).map(c => c.value);
    
    btnLoadTrends.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Loading Trends...';
    btnLoadTrends.disabled = true;
    
    const queryParams = new URLSearchParams({
        machineId: selectedMachineId,
        date: paramDate.value,
        shift: paramShift.value,
        startTime: paramStartTime.value,
        endTime: paramEndTime.value,
        fields: fields.join(',')
    });
    
    try {
        const response = await fetch(`/api/parameters?${queryParams}`);
        const data = await response.json();
        
        if (!data.success) {
            alert(`Error: ${data.error}`);
            return;
        }
        
        trendData = mergeTimeSeries(data.moulding, data.process);
        
        if (trendData.length === 0) {
            Object.values(chartContainers).forEach(c => c.style.display = 'none');
            Object.keys(activeCharts).forEach(cat => {
                if (activeCharts[cat]) {
                    activeCharts[cat].destroy();
                    activeCharts[cat] = null;
                }
            });
            tableCard.style.display = 'none';
            btnExportCSV.disabled = true;
            alert('No parameter records found matching the selected filters.');
            return;
        }
        
        // Display cards
        tableCard.style.display = 'block';
        btnExportCSV.disabled = false;
        
        currentPage = 1;
        renderTrendCharts(fields);
        renderTrendTable(fields);
        
    } catch (err) {
        console.error('Failed to load trend parameters:', err);
        alert('Failed to load trends data. Check console logs.');
    } finally {
        btnLoadTrends.innerHTML = '<i class="fa-solid fa-rotate"></i> Load Parameter Trends';
        btnLoadTrends.disabled = false;
    }
}

function getFieldCategory(fieldId) {
    for (const [cat, params] of Object.entries(mouldingParams)) {
        if (params.some(p => p.id === fieldId)) return cat;
    }
    for (const [cat, params] of Object.entries(processParams)) {
        if (params.some(p => p.id === fieldId)) return cat;
    }
    return null;
}

// Render separate line charts per category
function renderTrendCharts(selectedFields) {
    const categories = ['Pressure', 'Speed', 'Temperature', 'Timing'];
    
    categories.forEach(cat => {
        const catFields = selectedFields.filter(f => getFieldCategory(f) === cat);
        const container = chartContainers[cat];
        
        if (catFields.length === 0) {
            container.style.display = 'none';
            if (activeCharts[cat]) {
                activeCharts[cat].destroy();
                activeCharts[cat] = null;
            }
            return;
        }
        
        container.style.display = 'block';
        const canvasId = `canvas${cat}`;
        const canvas = document.getElementById(canvasId);
        const ctx = canvas.getContext('2d');
        
        if (activeCharts[cat]) {
            activeCharts[cat].destroy();
        }
        
        const labels = trendData.map(d => {
            const dt = new Date(d.TimeStamp);
            return dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        });
        
        const colors = [
            '#3b82f6', // blue
            '#06b6d4', // cyan
            '#10b981', // green
            '#f59e0b', // amber
            '#ec4899', // pink
            '#8b5cf6', // purple
            '#ef4444'  // red
        ];
        
        const datasets = catFields.map((field, idx) => {
            let label = field;
            const allLists = [...Object.values(mouldingParams).flat(), ...Object.values(processParams).flat()];
            const matched = allLists.find(p => p.id === field);
            if (matched) label = matched.label;
            
            const dataValues = trendData.map(d => d[field] !== undefined ? d[field] : null);
            const color = colors[idx % colors.length];
            
            return {
                label: label,
                data: dataValues,
                borderColor: color,
                backgroundColor: color + '1a',
                borderWidth: 2,
                pointRadius: trendData.length > 50 ? 0 : 3,
                pointHoverRadius: 5,
                tension: 0.15,
                spanGaps: true
            };
        });
        
        const isLight = document.body.classList.contains('light-theme');
        const gridColor = isLight ? 'rgba(0, 0, 0, 0.05)' : 'rgba(255, 255, 255, 0.04)';
        const textColor = isLight ? 'rgba(15, 23, 42, 0.7)' : 'rgba(255, 255, 255, 0.7)';
        const tickColor = isLight ? 'rgba(15, 23, 42, 0.5)' : 'rgba(255, 255, 255, 0.5)';
        const tooltipBg = isLight ? 'rgba(255, 255, 255, 0.95)' : 'rgba(15, 23, 42, 0.95)';
        const tooltipText = isLight ? '#0f172a' : '#fff';
        const tooltipBorder = isLight ? 'rgba(0, 0, 0, 0.1)' : 'rgba(255, 255, 255, 0.1)';
        
        activeCharts[cat] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: labels,
                datasets: datasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            color: textColor,
                            font: { family: 'Outfit', size: 11, weight: '500' },
                            boxWidth: 12
                        }
                    },
                    tooltip: {
                        backgroundColor: tooltipBg,
                        titleColor: tooltipText,
                        bodyColor: isLight ? 'rgba(15, 23, 42, 0.8)' : 'rgba(255,255,255,0.8)',
                        borderColor: tooltipBorder,
                        borderWidth: 1,
                        titleFont: { family: 'Outfit', size: 12, weight: 'bold' },
                        bodyFont: { family: 'Outfit', size: 11 }
                    }
                },
                scales: {
                    x: {
                        grid: { color: gridColor },
                        ticks: { color: tickColor, font: { family: 'Outfit', size: 10 } }
                    },
                    y: {
                        grid: { color: gridColor },
                        ticks: { color: tickColor, font: { family: 'Outfit', size: 10 } }
                    }
                }
            }
        });
    });
}

// Render paginated data table
function renderTrendTable(selectedFields) {
    // Generate headers
    trendTableHeader.innerHTML = '<th>Timestamp</th>';
    selectedFields.forEach(field => {
        let label = field;
        const allLists = [...Object.values(mouldingParams).flat(), ...Object.values(processParams).flat()];
        const matched = allLists.find(p => p.id === field);
        if (matched) label = matched.label;
        
        const th = document.createElement('th');
        th.textContent = label;
        trendTableHeader.appendChild(th);
    });
    
    // Paginated render
    const startIndex = (currentPage - 1) * rowsPerPage;
    const endIndex = Math.min(startIndex + rowsPerPage, trendData.length);
    
    trendTableBody.innerHTML = '';
    for (let i = startIndex; i < endIndex; i++) {
        const row = trendData[i];
        const tr = document.createElement('tr');
        
        const dt = new Date(row.TimeStamp);
        const timeStr = dt.toLocaleDateString() + ' ' + dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
        
        let html = `<td>${timeStr}</td>`;
        selectedFields.forEach(field => {
            const val = row[field];
            html += `<td>${val !== undefined && val !== null ? val : '-'}</td>`;
        });
        
        tr.innerHTML = html;
        trendTableBody.appendChild(tr);
    }
    
    renderPaginationControls();
}

// Pagination controls UI builder
function renderPaginationControls() {
    tablePagination.innerHTML = '';
    const totalPages = Math.ceil(trendData.length / rowsPerPage);
    
    const summary = document.createElement('span');
    summary.className = 'pagination-summary';
    summary.textContent = `Showing ${(currentPage-1)*rowsPerPage+1}-${Math.min(currentPage*rowsPerPage, trendData.length)} of ${trendData.length}`;
    tablePagination.appendChild(summary);
    
    // Prev Button
    const prevBtn = document.createElement('button');
    prevBtn.className = 'page-btn';
    prevBtn.innerHTML = '<i class="fa-solid fa-angle-left"></i>';
    prevBtn.disabled = currentPage === 1;
    prevBtn.addEventListener('click', () => {
        currentPage--;
        const fields = Array.from(document.querySelectorAll('.param-checkbox:checked')).map(c => c.value);
        renderTrendTable(fields);
    });
    tablePagination.appendChild(prevBtn);
    
    // Pages buttons (max 5 visible)
    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(totalPages, startPage + 4);
    
    for (let p = startPage; p <= endPage; p++) {
        const pBtn = document.createElement('button');
        pBtn.className = `page-btn ${p === currentPage ? 'active' : ''}`;
        pBtn.textContent = p;
        pBtn.addEventListener('click', () => {
            currentPage = p;
            const fields = Array.from(document.querySelectorAll('.param-checkbox:checked')).map(c => c.value);
            renderTrendTable(fields);
        });
        tablePagination.appendChild(pBtn);
    }
    
    // Next Button
    const nextBtn = document.createElement('button');
    nextBtn.className = 'page-btn';
    nextBtn.innerHTML = '<i class="fa-solid fa-angle-right"></i>';
    nextBtn.disabled = currentPage === totalPages;
    nextBtn.addEventListener('click', () => {
        currentPage++;
        const fields = Array.from(document.querySelectorAll('.param-checkbox:checked')).map(c => c.value);
        renderTrendTable(fields);
    });
    tablePagination.appendChild(nextBtn);
}

// Export CSV utility
function exportToCSV() {
    if (trendData.length === 0) return;
    
    const checkboxes = document.querySelectorAll('.param-checkbox:checked');
    const selectedFields = Array.from(checkboxes).map(c => c.value);
    
    const headers = ['Timestamp'];
    selectedFields.forEach(field => {
        let label = field;
        const allLists = [...Object.values(mouldingParams).flat(), ...Object.values(processParams).flat()];
        const matched = allLists.find(p => p.id === field);
        if (matched) label = matched.label;
        headers.push(label);
    });
    
    let csvContent = "data:text/csv;charset=utf-8," 
        + headers.map(h => `"${h.replace(/"/g, '""')}"`).join(",") + "\n";
        
    trendData.forEach(row => {
        const dt = new Date(row.TimeStamp);
        const timeStr = dt.toLocaleString();
        
        const line = [timeStr];
        selectedFields.forEach(field => {
            const val = row[field];
            line.push(val !== undefined && val !== null ? val : '');
        });
        
        csvContent += line.map(v => `"${v.toString().replace(/"/g, '""')}"`).join(",") + "\n";
    });
    
    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `ParameterTrends_${selectedMachineId}_${paramDate.value}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

// View switcher
function switchTab(viewName) {
    if (viewName === 'dashboard') {
        navDashboardBtn.classList.add('active');
        navParametersBtn.classList.remove('active');
        dashboardView.classList.add('active');
        parametersView.classList.remove('active');
        startAutoRefresh(); // Resume auto update on dashboard
    } else {
        navDashboardBtn.classList.remove('active');
        navParametersBtn.classList.add('active');
        dashboardView.classList.remove('active');
        parametersView.classList.add('active');
        clearInterval(countdownInterval); // Pause dashboard auto update
        
        // Initialize Parameter view defaults
        if (!paramDate.value) {
            const today = new Date().toISOString().split('T')[0];
            paramDate.value = today;
        }
        renderMachineSlider();
    }
}

// Set up UI Event listeners for Parameters view
function setupParametersListeners() {
    renderParameterSwitches();
    
    // Slider horizontal scroll arrows
    sliderPrevBtn.addEventListener('click', () => {
        machineCardsSlider.scrollBy({ left: -200, behavior: 'smooth' });
    });
    sliderNextBtn.addEventListener('click', () => {
        machineCardsSlider.scrollBy({ left: 200, behavior: 'smooth' });
    });
    
    // Category Selector link events (All/None group selects)
    document.querySelectorAll('.select-group-link').forEach(link => {
        link.addEventListener('click', (e) => {
            const cat = e.target.dataset.cat;
            const action = e.target.dataset.action;
            const grid = document.getElementById(`grid${cat}`);
            const checkboxes = grid.querySelectorAll('.param-checkbox');
            
            checkboxes.forEach(cb => {
                cb.checked = (action === 'all');
                const label = cb.closest('.switch-container');
                if (action === 'all') {
                    label.classList.add('checked');
                } else {
                    label.classList.remove('checked');
                }
            });

            // Auto reload for category toggle
            const checkedCount = document.querySelectorAll('.param-checkbox:checked').length;
            if (checkedCount > 0) {
                loadTrendsData();
            } else {
                Object.values(chartContainers).forEach(c => c.style.display = 'none');
                tableCard.style.display = 'none';
                btnExportCSV.disabled = true;
                Object.keys(activeCharts).forEach(catName => {
                    if (activeCharts[catName]) {
                        activeCharts[catName].destroy();
                        activeCharts[catName] = null;
                    }
                });
            }
        });
    });
    
    // Global Select/Clear All
    document.getElementById('btnSelectAllParams').addEventListener('click', () => {
        document.querySelectorAll('.param-checkbox').forEach(cb => {
            cb.checked = true;
            cb.closest('.switch-container').classList.add('checked');
        });
        loadTrendsData();
    });
    
    document.getElementById('btnClearAllParams').addEventListener('click', () => {
        document.querySelectorAll('.param-checkbox').forEach(cb => {
            cb.checked = false;
            cb.closest('.switch-container').classList.remove('checked');
        });
        Object.values(chartContainers).forEach(c => c.style.display = 'none');
        tableCard.style.display = 'none';
        btnExportCSV.disabled = true;
        Object.keys(activeCharts).forEach(cat => {
            if (activeCharts[cat]) {
                activeCharts[cat].destroy();
                activeCharts[cat] = null;
            }
        });
    });
    
    // Auto-update on filter input changes
    [paramDate, paramShift, paramStartTime, paramEndTime].forEach(input => {
        input.addEventListener('change', () => {
            const checkedCount = document.querySelectorAll('.param-checkbox:checked').length;
            if (selectedMachineId && checkedCount > 0) {
                loadTrendsData();
            }
        });
    });

    // Load trends button
    btnLoadTrends.addEventListener('click', loadTrendsData);
    
    // CSV export button
    btnExportCSV.addEventListener('click', exportToCSV);
    
    // Tab buttons
    navDashboardBtn.addEventListener('click', () => switchTab('dashboard'));
    navParametersBtn.addEventListener('click', () => switchTab('parameters'));
}

// Show error messages in grid area
function showErrorMessage(message) {
    machinesGrid.innerHTML = `
        <div class="glass-panel" style="grid-column: 1/-1; padding: 3rem; text-align: center; border-color: rgba(244, 63, 94, 0.3);">
            <i class="fa-solid fa-triangle-exclamation text-error" style="font-size: 3rem; margin-bottom: 1rem;"></i>
            <h3 class="text-error" style="margin-bottom: 0.5rem;">Dashboard Sync Error</h3>
            <p style="color: var(--text-secondary);">${message}</p>
        </div>
    `;
}

// Auto Refresh Logic
function startAutoRefresh() {
    clearInterval(countdownInterval);
    currentCountdown = refreshSeconds;
    countdownEl.textContent = currentCountdown;
    
    countdownInterval = setInterval(() => {
        currentCountdown--;
        countdownEl.textContent = currentCountdown;
        
        if (currentCountdown <= 0) {
            fetchStatus();
            currentCountdown = refreshSeconds;
        }
    }, 1000);
}

// Event Listeners
btnRefresh.addEventListener('click', () => {
    fetchStatus();
    startAutoRefresh();
});

txtSearch.addEventListener('input', (e) => {
    searchQuery = e.target.value;
    renderGrid();
});

filterTabs.forEach(tab => {
    tab.addEventListener('click', (e) => {
        filterTabs.forEach(t => t.classList.remove('active'));
        e.currentTarget.classList.add('active');
        currentFilter = e.currentTarget.dataset.filter;
        renderGrid();
    });
});

// Init Page
setupParametersListeners();
fetchStatus();
startAutoRefresh();

// Theme Toggle Logic
const btnThemeToggle = document.getElementById('btnThemeToggle');
const themeIcon = document.getElementById('themeIcon');

// Load saved theme or default to dark
const savedTheme = localStorage.getItem('theme') || 'dark';
if (savedTheme === 'light') {
    document.body.classList.add('light-theme');
    themeIcon.className = 'fa-solid fa-sun';
}

btnThemeToggle.addEventListener('click', () => {
    if (document.body.classList.contains('light-theme')) {
        document.body.classList.remove('light-theme');
        themeIcon.className = 'fa-solid fa-moon';
        localStorage.setItem('theme', 'dark');
    } else {
        document.body.classList.add('light-theme');
        themeIcon.className = 'fa-solid fa-sun';
        localStorage.setItem('theme', 'light');
    }
    // Update chart colors in real-time
    const isLight = document.body.classList.contains('light-theme');
    const gridColor = isLight ? 'rgba(0, 0, 0, 0.05)' : 'rgba(255, 255, 255, 0.04)';
    const textColor = isLight ? 'rgba(15, 23, 42, 0.7)' : 'rgba(255, 255, 255, 0.7)';
    const tickColor = isLight ? 'rgba(15, 23, 42, 0.5)' : 'rgba(255, 255, 255, 0.5)';
    const tooltipBg = isLight ? 'rgba(255, 255, 255, 0.95)' : 'rgba(15, 23, 42, 0.95)';
    const tooltipText = isLight ? '#0f172a' : '#fff';
    const tooltipBorder = isLight ? 'rgba(0, 0, 0, 0.1)' : 'rgba(255, 255, 255, 0.1)';

    Object.keys(activeCharts).forEach(cat => {
        const chart = activeCharts[cat];
        if (chart) {
            chart.options.scales.x.grid.color = gridColor;
            chart.options.scales.x.ticks.color = tickColor;
            chart.options.scales.y.grid.color = gridColor;
            chart.options.scales.y.ticks.color = tickColor;
            chart.options.plugins.legend.labels.color = textColor;
            chart.options.plugins.tooltip.backgroundColor = tooltipBg;
            chart.options.plugins.tooltip.titleColor = tooltipText;
            chart.options.plugins.tooltip.bodyColor = isLight ? 'rgba(15, 23, 42, 0.8)' : 'rgba(255,255,255,0.8)';
            chart.options.plugins.tooltip.borderColor = tooltipBorder;
            chart.update();
        }
    });
});

