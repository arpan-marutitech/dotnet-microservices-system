#!/bin/bash

# ============================================================================
# KIBANA INDEX PATTERN INITIALIZATION SCRIPT
# ============================================================================
# This script creates index patterns and basic visualizations in Kibana
# after Kibana and Elasticsearch have started.
# ============================================================================

set -e

KIBANA_URL="http://kibana:5601"
ES_URL="http://elasticsearch:9200"
ES_USER="elastic"
ES_PASSWORD="elastic123"

# Wait for Kibana to be ready
echo "⏳ Waiting for Kibana to be ready..."
for i in {1..30}; do
  if curl -s -f -u "$ES_USER:$ES_PASSWORD" "$KIBANA_URL/api/status" > /dev/null 2>&1; then
    echo "✅ Kibana is ready!"
    break
  fi
  echo "🔄 Attempt $i/30... Kibana not ready yet"
  sleep 2
done

# Wait for Elasticsearch to have data
echo "⏳ Waiting for Elasticsearch indices..."
sleep 10

# Create index pattern for logs
echo "📊 Creating index pattern for logs-*..."
curl -s -X POST "$KIBANA_URL/api/saved_objects/index-pattern/logs-*" \
  -u "$ES_USER:$ES_PASSWORD" \
  -H "Content-Type: application/json" \
  -H "kbn-xsrf: true" \
  -d '{
    "attributes": {
      "title": "logs-*",
      "timeFieldName": "@timestamp",
      "fields": "[]"
    }
  }' || echo "Index pattern may already exist"

# Create index pattern for error logs
echo "📊 Creating index pattern for logs-errors-*..."
curl -s -X POST "$KIBANA_URL/api/saved_objects/index-pattern/logs-errors-*" \
  -u "$ES_USER:$ES_PASSWORD" \
  -H "Content-Type: application/json" \
  -H "kbn-xsrf: true" \
  -d '{
    "attributes": {
      "title": "logs-errors-*",
      "timeFieldName": "@timestamp",
      "fields": "[]"
    }
  }' || echo "Index pattern may already exist"

echo "✅ Kibana initialization complete!"
