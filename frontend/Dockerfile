# Multi-stage build for Jellyfin frontend
FROM node:20 AS build

# Set working directory
WORKDIR /app

# Copy package files first for caching
COPY frontend/package.json frontend/package-lock.json ./
RUN npm install

# Copy the rest of the frontend files
COPY frontend/ .

# Copy git repository info if available (for commit SHA)
COPY .git/ ./.git/

# Build the Webpack project
RUN npm run build:production -- --no-type-checking

# Production stage with nginx
FROM nginx:alpine AS production

# Copy built files from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx configuration
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
