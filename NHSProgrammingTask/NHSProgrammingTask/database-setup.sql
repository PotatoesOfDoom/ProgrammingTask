CREATE DATABASE patientdb;

CREATE TABLE patients (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255),
    age INT,
    dob DATE,
    nhs_number VARCHAR(20)
);
