-- ============================================
-- Database Initialization Script
-- ============================================
-- This script creates all necessary databases for the microservices
-- It runs automatically when PostgreSQL container starts for the first time

-- Drop existing databases (if any)
DROP DATABASE IF EXISTS auth_db;
DROP DATABASE IF EXISTS user_db;
DROP DATABASE IF EXISTS product_db;
DROP DATABASE IF EXISTS order_db;
DROP DATABASE IF EXISTS payment_db;
DROP DATABASE IF EXISTS inventory_db;
DROP DATABASE IF EXISTS discount_db;
DROP DATABASE IF EXISTS notification_db;

-- Create databases for each service
CREATE DATABASE auth_db;
CREATE DATABASE user_db;
CREATE DATABASE product_db;
CREATE DATABASE order_db;
CREATE DATABASE payment_db;
CREATE DATABASE inventory_db;
CREATE DATABASE discount_db;
CREATE DATABASE notification_db;

-- Grant all privileges to postgres user
GRANT ALL PRIVILEGES ON DATABASE auth_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE user_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE product_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE order_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE payment_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE inventory_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE discount_db TO postgres;
GRANT ALL PRIVILEGES ON DATABASE notification_db TO postgres;

-- Log completion
\echo 'All databases created successfully!'
