import { useState } from 'react';

function App() {
  const [transcript, setTranscript] = useState('');
  const [patients, setPatients] = useState([]);


  const handleTranscriptChange = (event) => {
    setTranscript(event.target.value);
  };

  const handleSubmit = async (event) => {
    event.preventDefault();

    try {
      const response = await fetch('http://localhost:5053/api/PatientData', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(transcript), 
      });

      if (response.ok) {
        const data = await response.json();
        setPatients(data); 
      } else {
        console.error('Failed to fetch patients data');
      }
    } catch (error) {
      console.error('Error:', error);
    }
  };


  const formatDate = (date) => {
    return date ? new Date(date).toLocaleDateString() : 'N/A';
  };


  const formatField = (value) => {
    return value ? value : 'N/A';
  };

  return (
    <div>
      <h1>Patient Information</h1>

      <form onSubmit={handleSubmit}>
        <textarea
          value={transcript}
          onChange={handleTranscriptChange}
          placeholder="Enter transcript here..."
          style={{
            width: '90%',      
            height: '200px',     
            padding: '10px',     
            marginTop: '10px',   
            margineft: "50px",
            resize: 'vertical',
          }}
        />
        <button type="submit" style={{ marginTop: '10px' }}>Submit</button>
      </form>

      {/* Display patients in a table */}
      {patients.length > 0 && (
        <table border="1" style={{ marginTop: '20px', width: '100%' }}>
          <thead>
            <tr>
              <th>Name</th>
              <th>Age</th>
              <th>NHS Number</th>
              <th>Date of Birth</th>
            </tr>
          </thead>
          <tbody>
            {patients.map((patient, index) => (
              <tr key={index}>
                <td>{formatField(patient.name)}</td>
                <td>{formatField(patient.age)}</td>
                <td>{formatField(patient.nhsNumber)}</td>
                <td>{formatDate(patient.dob)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

export default App;
