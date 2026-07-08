const express = require('express');
const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');
const sql = require('mssql/msnodesqlv8');

const app = express();
const PORT = process.env.PORT || 3080;

// Configuration path
const xmlPath = path.join(__dirname, '../TBin2DbConsole/Data/MachineConfiguration.xml');

// Database configuration using connection string for ODBC Driver 17
const dbConfig = {
    connectionString: 'Driver={ODBC Driver 17 for SQL Server};Server=.\\SQLEXPRESS;Database=PPMS_LILBawal;Trusted_Connection=yes;'
};

// Global DB Connection Pool
let globalPool = null;

async function getDbPool() {
    if (globalPool) {
        return globalPool;
    }
    try {
        globalPool = await sql.connect(dbConfig);
        console.log('Successfully connected to PPMS_LILBawal database.');
        return globalPool;
    } catch (err) {
        console.error('Database connection failed:', err.message);
        globalPool = null;
        return null;
    }
}

// Ping helper using native windows ping command
function pingMachine(ip) {
    return new Promise((resolve) => {
        if (!ip || ip.trim() === '') return resolve(false);
        // Windows ping command: 1 packet (-n 1), 1000ms timeout (-w 1000)
        exec(`ping -n 1 -w 1000 ${ip}`, (error, stdout) => {
            if (error) {
                resolve(false);
            } else {
                const isOnline = stdout.includes("TTL=") || 
                                (stdout.includes("Received = 1") && !stdout.includes("Destination host unreachable"));
                resolve(isOnline);
            }
        });
    });
}

// Parse XML using regex to avoid extra package dependencies
function getMachinesFromXml() {
    try {
        if (!fs.existsSync(xmlPath)) {
            console.error(`XML file not found at: ${xmlPath}`);
            return [];
        }
        const xmlContent = fs.readFileSync(xmlPath, 'utf8');
        const machines = [];
        const machineRegex = /<MachineDetails\s+([^>]+)\s*\/>/g;
        let match;
        while ((match = machineRegex.exec(xmlContent)) !== null) {
            const attrString = match[1];
            const attrs = {};
            const attrRegex = /(\w+)="([^"]*)"/g;
            let attrMatch;
            while ((attrMatch = attrRegex.exec(attrString)) !== null) {
                attrs[attrMatch[1]] = attrMatch[2];
            }
            if (attrs.Machine_ID) {
                machines.push({
                    id: attrs.Machine_ID,
                    ip: attrs.Machine_IP || '',
                    ftpPath: attrs.Machine_Ftp_Path || '',
                    tacTime: parseInt(attrs.TacTime || '60', 10)
                });
            }
        }
        return machines;
    } catch (err) {
        console.error('Error parsing XML:', err);
        return [];
    }
}

// Express static files
app.use(express.static(path.join(__dirname, 'public')));

// State tracking for machine online/offline transitions
const machineStates = {};

// Helper to format Date into YYYY-MM-DD HH:MM:SS local string
function formatLocalDateTime(date) {
    if (!date) return null;
    const pad = (n) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}

// API Endpoint to get status
app.get('/api/status', async (req, res) => {
    const machines = getMachinesFromXml();
    if (machines.length === 0) {
        return res.status(500).json({ success: false, error: 'No machines configured in XML.' });
    }

    const pool = await getDbPool();
    const results = [];

    // Perform queries and pings in parallel
    const promises = machines.map(async (machine) => {
        // 1. Ping test
        const isOnline = await pingMachine(machine.ip);

        // 1.5. State tracking & transition checks
        let state = machineStates[machine.id];
        if (!state) {
            state = {
                lastOnlineStatus: isOnline,
                lastOfflineTime: null,
                lastOnlineTime: isOnline ? new Date() : null
            };
            machineStates[machine.id] = state;
        } else {
            if (state.lastOnlineStatus && !isOnline) {
                state.lastOfflineTime = new Date();
            } else if (!state.lastOnlineStatus && isOnline) {
                state.lastOnlineTime = new Date();
            }
            state.lastOnlineStatus = isOnline;
        }

        // 2. Database query
        let dbData = {
            process: null,
            machine: null,
            alarm: null
        };

        if (pool) {
            try {
                // Get latest process data
                const processResult = await pool.request()
                    .input('machineId', sql.VarChar, machine.id)
                    .query(`
                        SELECT TOP 1 
                            NID, 
                            CONVERT(varchar, Date_Time, 120) AS Date_Time, 
                            Shot_Count, 
                            Cycle_Time, 
                            Injection_Time, 
                            Dosing_Time, 
                            Melt_Cushion, 
                            CONVERT(varchar, TimeStamp, 120) AS TimeStamp, 
                            ShiftName, 
                            CONVERT(varchar, ProdDate, 23) AS ProdDate
                        FROM dbo.Machine_Process_Data 
                        WHERE Machine_Id = @machineId 
                        ORDER BY NID DESC
                    `);

                // Get latest machine parameters
                const machineResult = await pool.request()
                    .input('machineId', sql.VarChar, machine.id)
                    .query(`
                        SELECT TOP 1 
                            Mould_ID, Part_Number, Material_Name, Total_Shots, Ideal_Cycle_Time
                        FROM dbo.Machine_Data 
                        WHERE Machine_Id = @machineId 
                        ORDER BY NID DESC
                    `);

                // Get latest 4 alarms
                const alarmResult = await pool.request()
                    .input('machineId', sql.VarChar, machine.id)
                    .query(`
                        SELECT TOP 4 
                            Alarm_Number, Alarm_Status, Set_Date_Time, Reset_Date_Time
                        FROM dbo.Machine_Alarm_Data 
                        WHERE Machine_Id = @machineId 
                        ORDER BY NID DESC
                    `);

                dbData.process = processResult.recordset[0] || null;
                dbData.machine = machineResult.recordset[0] || null;
                dbData.alarms = alarmResult.recordset || [];
                dbData.alarm  = dbData.alarms[0] || null;
            } catch (queryErr) {
                console.error(`Error querying database for machine ${machine.id}:`, queryErr.message);
            }
        }

        results.push({
            id: machine.id,
            ip: machine.ip,
            ftpPath: machine.ftpPath,
            isOnline: isOnline,
            lastOfflineTime: formatLocalDateTime(state.lastOfflineTime),
            lastOnlineTime: formatLocalDateTime(state.lastOnlineTime),
            lastData: dbData
        });
    });

    await Promise.all(promises);

    // Sort machines by ID for consistency
    results.sort((a, b) => a.id.localeCompare(b.id));

    res.json({
        success: true,
        dbConnected: !!pool,
        timestamp: new Date(),
        machines: results
    });
});

// Whitelist of columns to select from each table
const mouldingFields = [
    'Injection_pressure_step_1', 'Injection_pressure_step_2', 'Injection_pressure_step_3', 'Injection_pressure_step_4',
    'Injection_speed_step_1', 'Injection_speed_step_2', 'Injection_speed_step_3', 'Injection_speed_step_4',
    'Injection_position_for_speed_1', 'Injection_position_for_speed_2', 'Injection_position_for_speed_3', 'Injection_position_for_speed_4',
    'Holding_pressure_step_1', 'Holding_pressure_step_2', 'Holding_pressure_step_3', 'Holding_pressure_step_4',
    'Holding_time_step_1', 'Holding_time_step_2', 'Holding_time_step_3', 'Holding_time_step_4',
    'Injection_time_actual', 'Cooling_time_actual', 'Dosing_time_actual', 'Dosing_speed_actual',
    'Dosing_speed_step_1', 'Dosing_speed_step_2', 'Dosing_speed_step_3',
    'Dosing_back_pressure_step_1', 'Dosing_back_pressure_step_2', 'Dosing_back_pressure_step_3',
    'Barrel_temperature_actual_nozzle', 'Barrel_temperature_actual_zone_1', 'Barrel_temperature_actual_zone_2',
    'Barrel_temperature_actual_zone_3', 'Barrel_temperature_actual_zone_4', 'Barrel_temperature_actual_zone_5',
    'Oil_temperature_actual',
    'Hot_runner_temperature_actual_zone_1', 'Hot_runner_temperature_actual_zone_2', 'Hot_runner_temperature_actual_zone_3',
    'Hot_runner_temperature_actual_zone_4', 'Hot_runner_temperature_actual_zone_5', 'Hot_runner_temperature_actual_zone_6',
    'Hot_runner_temperature_actual_zone_7', 'Hot_runner_temperature_actual_zone_8', 'Hot_runner_temperature_actual_zone_9',
    'Hot_runner_temperature_actual_zone_10', 'Hot_runner_temperature_actual_zone_11', 'Hot_runner_temperature_actual_zone_12',
    'Cascade_injection_delay_time_1', 'Cascade_injection_delay_time_2', 'Cascade_injection_delay_time_3',
    'Cascade_injection_delay_time_4', 'Cascade_injection_delay_time_5', 'Cascade_injection_delay_time_6',
    'Cascade_injection_delay_time_7', 'Cascade_injection_delay_time_8'
];

const processFields = [
    'Cycle_Time', 'Injection_Time', 'Dosing_Time', 'Dosing_Stop', 'Melt_Cushion', 'Switch_Over_Position',
    'Mold_Close_Time', 'Mold_Open_Time', 'Zone_3_Temperature', 'Nozzle_1_Temeprature', 'Feed_Temperature',
    'Zone_1_Temeprature', 'Zone_2_Temeprature', 'Zone_4_Temeprature', 'Oil_Temperature', 'Melt_Temperature',
    'Nozzle_2_Temperaturee', 'Switch_Over_Pressure', 'Tonnage', 'Mold_Open_Stop', 'Tonnage_Build_Time',
    'Tonnage_Release_Time', 'Ejector_Forward_Time', 'Ejector_Back_Time', 'Minimum_Melt_Cushion',
    'Injection_Start_Position', 'Peak_Injection_Pressure', 'Mold_Zone1_Temperature', 'Mold_Zone2_Temperature',
    'MTC_Temperature', 'DownTime'
];

app.get('/api/alarms/analytics', async (req, res) => {
    try {
        const { machineId, range, startDate, endDate } = req.query;
        const pool = await getDbPool();
        if (!pool) {
            return res.status(500).json({ success: false, error: 'Database connection failed.' });
        }
        
        let queryStr = `
            SELECT 
                Alarm_Number,
                COUNT(*) as Occurrence,
                SUM(DATEDIFF(second, Set_Date_Time, ISNULL(Reset_Date_Time, GETDATE()))) as Duration
            FROM dbo.Machine_Alarm_Data
            WHERE Set_Date_Time IS NOT NULL
        `;
        
        const request = pool.request();
        
        if (machineId) {
            queryStr += ` AND Machine_Id = @machineId`;
            request.input('machineId', sql.VarChar, machineId);
        }
        
        if (startDate && endDate) {
            queryStr += ` AND Set_Date_Time BETWEEN @startDate AND @endDate`;
            request.input('startDate', sql.DateTime, new Date(startDate));
            request.input('endDate', sql.DateTime, new Date(endDate));
        } else if (range) {
            if (range === 'Day') {
                queryStr += ` AND Set_Date_Time >= DATEADD(day, -1, GETDATE())`;
            } else if (range === 'Week') {
                queryStr += ` AND Set_Date_Time >= DATEADD(week, -1, GETDATE())`;
            } else if (range === 'Month') {
                queryStr += ` AND Set_Date_Time >= DATEADD(month, -1, GETDATE())`;
            } else if (range === 'Shift') {
                queryStr += ` AND Set_Date_Time >= DATEADD(hour, -8, GETDATE())`;
            }
        }
        
        queryStr += ` GROUP BY Alarm_Number`;
        
        const result = await request.query(queryStr);
        const records = result.recordset || [];
        
        // Sort and slice top 5 by Duration (Minutes)
        const topDuration = [...records]
            .sort((a, b) => b.Duration - a.Duration)
            .slice(0, 5)
            .map(r => ({
                alarm: `Alarm ${r.Alarm_Number}`,
                value: parseFloat((r.Duration / 60).toFixed(1))
            }));
            
        // Sort and slice top 5 by Occurrence
        const topOccurrence = [...records]
            .sort((a, b) => b.Occurrence - a.Occurrence)
            .slice(0, 5)
            .map(r => ({
                alarm: `Alarm ${r.Alarm_Number}`,
                value: r.Occurrence
            }));
            
        res.json({
            success: true,
            topDuration,
            topOccurrence
        });
    } catch (err) {
        console.error('Alarms analytics query error:', err.message);
        res.status(500).json({ success: false, error: err.message });
    }
});

app.get('/api/parameters', async (req, res) => {
    const { machineId, date, shift, startTime, endTime, fields } = req.query;

    if (!machineId || !date) {
        return res.status(400).json({ success: false, error: 'machineId and date parameters are required.' });
    }

    const pool = await getDbPool();
    if (!pool) {
        return res.status(500).json({ success: false, error: 'Database connection failed.' });
    }

    // Split requested fields and filter based on whitelist
    const requestedFields = fields ? fields.split(',') : [];
    const activeMouldingFields = requestedFields.filter(f => mouldingFields.includes(f));
    const activeProcessFields = requestedFields.filter(f => processFields.includes(f));

    if (activeMouldingFields.length === 0 && activeProcessFields.length === 0) {
        return res.status(400).json({ success: false, error: 'No valid parameters selected.' });
    }

    try {
        let mouldingData = [];
        let processData = [];

        // Build base condition for shift and start/end times
        let filterSql = "WHERE Machine_Id = @machineId AND ProdDate = @prodDate";
        if (shift && shift !== 'all') {
            filterSql += " AND ShiftName = @shift";
        }
        if (startTime) {
            filterSql += " AND CAST(TimeStamp AS TIME) >= @startTime";
        }
        if (endTime) {
            filterSql += " AND CAST(TimeStamp AS TIME) <= @endTime";
        }

        // Query Moulding Data if any moulding fields are selected
        if (activeMouldingFields.length > 0) {
            const selectCols = activeMouldingFields.map(col => `[${col}]`).join(', ');
            const mouldingQuery = `
                SELECT TimeStamp, ${selectCols} 
                FROM dbo.Machine_Moulding_Data 
                ${filterSql} 
                ORDER BY TimeStamp ASC
            `;

            const request = pool.request()
                .input('machineId', sql.VarChar, machineId)
                .input('prodDate', sql.VarChar, date);
            if (shift && shift !== 'all') request.input('shift', sql.VarChar, shift);
            if (startTime) request.input('startTime', sql.VarChar, startTime);
            if (endTime) request.input('endTime', sql.VarChar, endTime);

            const result = await request.query(mouldingQuery);
            mouldingData = result.recordset;
        }

        // Query Process Data if any process fields are selected
        if (activeProcessFields.length > 0) {
            const selectCols = activeProcessFields.map(col => `[${col}]`).join(', ');
            const processQuery = `
                SELECT TimeStamp, ${selectCols} 
                FROM dbo.Machine_Process_Data 
                ${filterSql} 
                ORDER BY TimeStamp ASC
            `;

            const request = pool.request()
                .input('machineId', sql.VarChar, machineId)
                .input('prodDate', sql.VarChar, date);
            if (shift && shift !== 'all') request.input('shift', sql.VarChar, shift);
            if (startTime) request.input('startTime', sql.VarChar, startTime);
            if (endTime) request.input('endTime', sql.VarChar, endTime);

            const result = await request.query(processQuery);
            processData = result.recordset;
        }

        // Function to safely parse varchar values to floats for visualization
        const cleanRow = (row, cols) => {
            const cleaned = { TimeStamp: row.TimeStamp };
            cols.forEach(col => {
                const val = row[col];
                if (val === null || val === undefined || val === '') {
                    cleaned[col] = null;
                } else {
                    const parsed = parseFloat(val);
                    cleaned[col] = isNaN(parsed) ? val : parsed;
                }
            });
            return cleaned;
        };

        const cleanedMoulding = mouldingData.map(r => cleanRow(r, activeMouldingFields));
        const cleanedProcess = processData.map(r => cleanRow(r, activeProcessFields));

        res.json({
            success: true,
            moulding: cleanedMoulding,
            process: cleanedProcess
        });

    } catch (err) {
        console.error('Error fetching parameters data:', err);
        res.status(500).json({ success: false, error: err.message });
    }
});


app.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});
