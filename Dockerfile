# Use an official Python runtime as a parent image
FROM python:3-slim

# Set the working directory in the container
WORKDIR /app

# Copy the dependencies file to the working directory
COPY requirements.txt .

# Install any needed dependencies
# Ensure build essentials are available for markitdown's dependencies if any
RUN apt-get update && apt-get install -y build-essential && \
    pip install --no-cache-dir -r requirements.txt && \
    apt-get purge -y --auto-remove build-essential && \
    rm -rf /var/lib/apt/lists/*

# Copy the content of the local src directory to the working directory
COPY app.py . 
COPY jina_handler.py . 
COPY pdf_handler.py . 

# Make port 3002 available to the world outside this container
EXPOSE 3002

# Run app.py
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "3002", "--workers", "2"]