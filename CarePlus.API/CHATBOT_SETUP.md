# CarePlus RAG chatbot setup

The chatbot uses two cloud services:

- Groq generates answers.
- Qdrant Cloud Inference creates text embeddings and stores/searches the vectors.

Secrets are read from environment variables and must not be committed.

## 1. Create Qdrant Cloud

Create a Qdrant Cloud cluster, enable Cloud Inference, and create a Database API key with write access. This implementation uses Qdrant Cloud Inference, so it does not require Docker or a separate embedding provider.

## 2. Configure the API in PowerShell

Set these in the same PowerShell window that will run `dotnet run`:

```powershell
$env:Groq__ApiKey="your-groq-api-key"
$env:Groq__Model="your-current-groq-model-id"
$env:Qdrant__Url="https://your-cluster.cloud.qdrant.io:6333"
$env:Qdrant__ApiKey="your-qdrant-api-key"
```

The default Qdrant Cloud Inference model is `sentence-transformers/all-minilm-l6-v2`. It is free on free-tier Qdrant Cloud clusters and produces 384-dimension vectors. If you change it, also change `Qdrant__VectorSize`, then recreate the collection.

## 3. Index the bundled knowledge

The `KnowledgeBase` folder contains separate approved documents for appointments, records, medicine and orders, billing and payments, accounts and security, family and notifications, and public clinic information. Keeping these topics in focused sections improves retrieval for short questions such as "how to make booking".

Run the API, sign in as an Admin, and call:

```http
POST /api/chat/knowledge/bundled
Authorization: Bearer <admin-token>
```

You can index or replace a custom document with:

```http
POST /api/chat/knowledge
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "documentId": "clinic-faq",
  "title": "Clinic FAQ",
  "source": "Approved clinic FAQ",
  "content": "The approved source text goes here."
}
```

Using the same `documentId` replaces the old chunks instead of creating duplicates.

