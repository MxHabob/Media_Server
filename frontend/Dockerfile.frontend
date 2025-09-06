FROM node:16 AS build

# Set working directory
WORKDIR /app

# Copy package files first for caching
COPY frontend/package.json frontend/package-lock.json ./
RUN npm install

# Copy the rest of the frontend files
COPY frontend/ .

# Build the Webpack project
RUN npm run build:production
