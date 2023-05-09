FROM python:3.9

WORKDIR /app

COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

COPY firmware_update_microservice.py ./

CMD ["python", "./firmware-broker.py"]
