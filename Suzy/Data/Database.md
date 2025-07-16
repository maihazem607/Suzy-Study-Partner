
This is the structure of the database 



## 📂 Collections Overview

1. `users`
2. `studyMaterials`
3. `flashcardDecks`
4. `mockExams`
5. `studySessions`
6. `goals`
7. `toDoLists`
8. `notifications`
9. `aiInsights`
10. `leaderboard`

---

## 📘 `users`

```json
{
  _id: ObjectId,
  name: String,
  email: String,
  password: String,
  profile: {
    preferredStudyTimes: [String],  // e.g., ["08:00-10:00"]
    studyMusicEnabled: Boolean
  },
  createdAt: Date,
  updatedAt: Date
}
```

---

## 📚 `studyMaterials`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  title: String,
  type: "pdf" | "image" | "text",
  url: String,
  tags: [String],
  subject: String,
  sharedWith: [ObjectId], // user IDs
  createdAt: Date
}
```

---

## 🧾 `flashcardDecks`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  name: String,
  subject: String,
  cards: [
    {
      cardId: String,
      front: String,
      back: String,
      mastered: Boolean
    }
  ],
  createdAt: Date,
  updatedAt: Date
}
```

---

## 📝 `mockExams`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  materialIds: [ObjectId], // link to studyMaterials
  title: String,
  difficulty: "easy" | "medium" | "hard",
  questionsCount: Number,
  score: Number,
  takenAt: Date,
  answers: [
    {
      question: String,
      answerGiven: String,
      isCorrect: Boolean
    }
  ]
}
```

---

## ⏳ `studySessions`

```json
{
  _id: ObjectId,
  hostUserId: ObjectId,
  type: "public" | "private",
  title: String,
  participants: [ObjectId],
  startedAt: Date,
  endedAt: Date,
  durationMinutes: Number,
  mood: String, // e.g., "focused", "distracted"
  focusLevel: Number, // 1–10
  topics: [String]
}
```

---

## 🎯 `goals`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  hoursPerWeek: Number,
  topicsToCover: [String],
  progress: {
    hoursStudied: Number,
    topicsCovered: [String]
  },
  createdAt: Date,
  updatedAt: Date
}
```

---

## ✅ `toDoLists`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  tasks: [
    {
      taskId: String,
      task: String,
      dueDate: Date,
      completed: Boolean
    }
  ],
  createdAt: Date
}
```

---

## 🔔 `notifications`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  remindersEnabled: Boolean,
  newSessionAlerts: Boolean,
  lastNotified: Date
}
```

---

## 🤖 `aiInsights`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  productivityTips: [String],
  attentionSpanMinutes: Number,
  peakHours: [String],
  updatedAt: Date
}
```

---

## 🏆 `leaderboard`

```json
{
  _id: ObjectId,
  userId: ObjectId,
  totalStudyMinutes: Number,
  rank: Number,
  lastUpdated: Date
}
```

---

## 🔁 Relationships Summary

* `studyMaterials`, `flashcardDecks`, `mockExams`, `goals`, `studySessions`, `toDoLists` are **linked to users via `userId`**.
* `studySessions` can reference multiple users (participants).
* `aiInsights` computed from session logs + user behaviors.

---