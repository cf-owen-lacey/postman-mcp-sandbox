import { useEffect, useState } from 'react'
import './App.css'

const API_BASE = 'http://localhost:5174/api';

function App() {
  const [flights, setFlights] = useState([]);
  const [reports, setReports] = useState([]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [selectedFlights, setSelectedFlights] = useState(new Set());
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  async function load() {
    setLoading(true);
    try {
      const [fRes, rRes] = await Promise.all([
        fetch(`${API_BASE}/flights`),
        fetch(`${API_BASE}/reports`)
      ]);
      if (!fRes.ok) throw new Error('Failed flights');
      if (!rRes.ok) throw new Error('Failed reports');
      setFlights(await fRes.json());
      setReports(await rRes.json());
      setError(null);
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => { load(); }, []);

  const toggleFlight = (id) => {
    const next = new Set(selectedFlights);
    if (next.has(id)) next.delete(id); else next.add(id);
    setSelectedFlights(next);
  };

  async function createReport(e) {
    e.preventDefault();
    if (!title.trim()) return;
    const body = {
      title: title.trim(),
      description: description.trim() || null,
      flightIds: Array.from(selectedFlights)
    };
    const res = await fetch(`${API_BASE}/reports`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    if (res.ok) {
      setTitle('');
      setDescription('');
      setSelectedFlights(new Set());
      await load();
    } else {
      alert('Failed to create report');
    }
  }

  async function updateReport(id, patch) {
    const res = await fetch(`${API_BASE}/reports/${id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(patch)
    });
    if (res.ok) load();
  }

  if (loading) return <p>Loading...</p>;
  if (error) return <p style={{color:'red'}}>Error: {error}</p>;

  return (
    <div className="app">
      <h1>Flight Reports</h1>
      <section>
        <h2>Create Report</h2>
        <form onSubmit={createReport}>
          <div>
            <label>Title <input value={title} onChange={e=>setTitle(e.target.value)} /></label>
          </div>
          <div>
            <label>Description <input value={description} onChange={e=>setDescription(e.target.value)} /></label>
          </div>
          <div>
            <strong>Select Flights:</strong>
            <ul>
              {flights.map(f => (
                <li key={f.id}>
                  <label>
                    <input type="checkbox" checked={selectedFlights.has(f.id)} onChange={()=>toggleFlight(f.id)} />
                    {f.number} {f.origin} → {f.destination} ({f.status})
                  </label>
                </li>
              ))}
            </ul>
          </div>
          <button type="submit">Create Report</button>
        </form>
      </section>
      <section>
        <h2>Existing Reports</h2>
        {reports.length === 0 && <p>No reports yet.</p>}
        <ul>
          {reports.map(r => (
            <li key={r.id}>
              <details>
                <summary>{r.title} ({r.flightIds.length} flights)</summary>
                <p>{r.description}</p>
                <p>Created: {r.createdUtc}</p>
                {r.updatedUtc && <p>Updated: {r.updatedUtc}</p>}
                <div>
                  <strong>Flights:</strong>
                  <ul>
                    {r.flightIds.map(fid => {
                      const f = flights.find(fl => fl.id === fid);
                      return <li key={fid}>{f ? `${f.number} ${f.origin}→${f.destination}` : fid}</li>
                    })}
                  </ul>
                </div>
                <button onClick={()=>updateReport(r.id,{ title: r.title + ' *' })}>Append *</button>
              </details>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}

export default App
