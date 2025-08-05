let timerInterval;
let timeLeft = 25 * 60; // 25 minutes in seconds
let isRunning = false;
let todos = [];
let sessionData = null;
let sessionId = null;
let isBreakMode = false;
let originalStudyTime = 25 * 60;
let originalBreakTime = 5 * 60;

// Make sessionData and sessionId available globally for navbar
window.sessionData = null;
window.sessionId = null;

// Get session ID from URL parameters
const urlParams = new URLSearchParams(window.location.search);
sessionId = urlParams.get('sessionId');
window.sessionId = sessionId;

// Timer functions
function updateDisplay() {
    const minutes = Math.floor(timeLeft / 60);
    const seconds = timeLeft % 60;
    document.getElementById('timerDisplay').textContent =
        `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
}

function setTimerDuration(studyMinutes, breakMinutes) {
    originalStudyTime = studyMinutes * 60;
    originalBreakTime = breakMinutes * 60;
    timeLeft = originalStudyTime;
    isBreakMode = false;
    updateDisplay();
    updateTimerMode();
}

function updateTimerMode() {
    const timerArea = document.querySelector('.timer-area');
    const breakBtn = document.getElementById('breakBtn');
    const body = document.body;

    if (isBreakMode) {
        timerArea.classList.add('break-mode');
        body.classList.add('break-mode');
        breakBtn.style.display = 'none';
        document.getElementById('timerTypeHeader').innerHTML =
            '<i class="fas fa-coffee me-2"></i>Break Time';
    } else {
        timerArea.classList.remove('break-mode');
        body.classList.remove('break-mode');
        breakBtn.style.display = timeLeft === 0 && !isRunning ? 'inline-block' : 'none';
        document.getElementById('timerTypeHeader').innerHTML =
            '<i class="fas fa-clock me-2"></i>Study Timer';
    }
}

async function startTimer() {
    if (!isRunning) {
        // Call backend to start timer session
        if (sessionId && !isBreakMode) {
            try {
                const response = await fetch(`/api/StudySession/StartSession/${sessionId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });
                if (response.ok) {
                    const result = await response.json();
                    window.currentTimerSessionId = result.timerSessionId;
                    console.log('Timer session started:', result.timerSessionId);
                } else {
                    console.error('Failed to start timer session');
                }
            } catch (error) {
                console.error('Error starting timer session:', error);
            }
        } else if (sessionId && isBreakMode) {
            try {
                const response = await fetch(`/api/StudySession/StartBreak/${sessionId}`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    }
                });
                if (response.ok) {
                    const result = await response.json();
                    window.currentTimerSessionId = result.timerSessionId;
                    console.log('Break timer started:', result.timerSessionId);
                } else {
                    console.error('Failed to start break timer');
                }
            } catch (error) {
                console.error('Error starting break timer:', error);
            }
        }

        isRunning = true;
        document.getElementById('startBtn').classList.add('active');
        timerInterval = setInterval(() => {
            if (timeLeft > 0) {
                timeLeft--;
                updateDisplay();
            } else {
                // Timer finished
                clearInterval(timerInterval);
                isRunning = false;
                document.getElementById('startBtn').classList.remove('active');

                // End the current timer session
                if (window.currentTimerSessionId) {
                    endTimerSession();
                }

                if (isBreakMode) {
                    alert('Break time is over! Ready to study?');
                    isBreakMode = false;
                    timeLeft = originalStudyTime;
                } else {
                    alert('Study session complete! Time for a break.');
                }
                updateDisplay();
                updateTimerMode();
            }
        }, 1000);
    }
}

async function pauseTimer() {
    if (isRunning) {
        clearInterval(timerInterval);
        isRunning = false;
        document.getElementById('startBtn').classList.remove('active');

        // End the current timer session when pausing
        if (window.currentTimerSessionId) {
            endTimerSession();
        }
    }
}

async function resetTimer() {
    clearInterval(timerInterval);

    // End current timer session if one is running
    if (isRunning && window.currentTimerSessionId) {
        endTimerSession();
    }

    isRunning = false;
    isBreakMode = false; // Reset break mode when resetting timer
    document.getElementById('startBtn').classList.remove('active');
    timeLeft = originalStudyTime;
    updateDisplay();
    updateTimerMode();
}

async function endTimerSession() {
    if (window.currentTimerSessionId) {
        try {
            const response = await fetch(`/api/StudySession/EndSession/${sessionId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                const result = await response.json();
                console.log('Timer session ended:', result);
                window.currentTimerSessionId = null;
            } else {
                console.error('Failed to end timer session');
            }
        } catch (error) {
            console.error('Error ending timer session:', error);
        }
    }
}

async function recordSessionEndTime() {
    if (sessionId) {
        try {
            const response = await fetch(`/api/StudySession/EndSession/${sessionId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                }
            });
            if (response.ok) {
                console.log('Session end time recorded');
            } else {
                console.error('Failed to record session end time');
            }
        } catch (error) {
            console.error('Error recording session end time:', error);
        }
    }
}

function startBreak() {
    isBreakMode = true;
    timeLeft = originalBreakTime;
    updateDisplay();
    updateTimerMode();
}

function setupEventListeners() {
    // Allow Enter key to save todo
    document.getElementById('newTodoInput').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            saveTodo();
        } else if (e.key === 'Escape') {
            cancelAddTodo();
        }
    });
}

// Todo functions
async function loadTodos() {
    try {
        const url = sessionId ? `/api/Todo/GetTodos?studySessionId=${sessionId}` : '/api/Todo/GetTodos';
        const response = await fetch(url);
        if (response.ok) {
            todos = await response.json();
            renderTodos();
        } else {
            console.error('Failed to load todos');
        }
    } catch (error) {
        console.error('Error loading todos:', error);
    }
}

function renderTodos() {
    const todoList = document.getElementById('todoList');

    if (todos.length === 0) {
        todoList.innerHTML = `
                                    <div class="text-center text-muted py-3">
                                        <i class="fas fa-tasks fa-2x mb-2"></i>
                                        <p>No tasks yet. Add your first task to get started!</p>
                                    </div>
                                `;
        return;
    }

    todoList.innerHTML = '';

    todos.forEach(todo => {
        const todoItem = document.createElement('div');
        todoItem.className = 'todo-item';
        todoItem.innerHTML = `
                                    <input type="checkbox" ${todo.isCompleted ? 'checked' : ''} 
                                            onchange="toggleTodo(${todo.id})">
                                    <span class="${todo.isCompleted ? 'todo-completed' : ''}" 
                                            ondblclick="editTodo(${todo.id})">${todo.task}</span>
                                    <button class="btn btn-sm btn-outline-danger ms-2" 
                                            onclick="deleteTodo(${todo.id})" style="font-size: 0.7rem;">
                                        <i class="fas fa-trash"></i>
                                    </button>
                                `;
        todoList.appendChild(todoItem);
    });
}

async function toggleTodo(id) {
    const todo = todos.find(t => t.id === id);
    if (!todo) return;

    try {
        const response = await fetch(`/api/Todo/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                isCompleted: !todo.isCompleted
            })
        });

        if (response.ok) {
            todo.isCompleted = !todo.isCompleted;
            todo.completedAt = todo.isCompleted ? new Date().toISOString() : null;
            renderTodos();
        } else {
            console.error('Failed to update todo');
        }
    } catch (error) {
        console.error('Error updating todo:', error);
    }
}

async function deleteTodo(id) {
    if (!confirm('Are you sure you want to delete this task?')) return;

    try {
        const response = await fetch(`/api/Todo/${id}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            todos = todos.filter(t => t.id !== id);
            renderTodos();
        } else {
            console.error('Failed to delete todo');
        }
    } catch (error) {
        console.error('Error deleting todo:', error);
    }
}

function showAddTodoForm() {
    document.getElementById('addTodoForm').style.display = 'flex';
    document.getElementById('newTodoInput').focus();
}

function cancelAddTodo() {
    document.getElementById('addTodoForm').style.display = 'none';
    document.getElementById('newTodoInput').value = '';
}

async function saveTodo() {
    const input = document.getElementById('newTodoInput');
    const task = input.value.trim();

    if (!task) return;

    try {
        const response = await fetch('/api/Todo/CreateTodo', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                task: task,
                studySessionId: sessionId,
                order: todos.length
            })
        });

        if (response.ok) {
            const newTodo = await response.json();
            todos.push(newTodo);
            renderTodos();
            cancelAddTodo();
        } else {
            console.error('Failed to create todo');
        }
    } catch (error) {
        console.error('Error creating todo:', error);
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    // Clean up any previous break mode state
    document.body.classList.remove('break-mode');

    if (sessionId) {
        loadSessionData();
    } else {
        // If no session ID, redirect to session manager
        window.location.href = '/Sessions/SessionManager';
        return;
    }

    loadTodos();
    setupEventListeners();
});

async function loadSessionData() {
    try {
        const response = await fetch(`/api/StudySession/${sessionId}`);
        if (response.ok) {
            sessionData = await response.json();
            window.sessionData = sessionData; // Make available globally
            updateUIWithSessionData();
        } else {
            alert('Failed to load session data. Redirecting to session manager.');
            window.location.href = '/Sessions/SessionManager';
        }
    } catch (error) {
        console.error('Error loading session data:', error);
        alert('Error loading session. Redirecting to session manager.');
        window.location.href = '/Sessions/SessionManager';
    }
}

function updateUIWithSessionData() {
    if (!sessionData) return;

    // Update page title
    document.title = `Study Session: ${sessionData.title}`;

    // Update navbar session title
    const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
    const sessionTitleText = document.getElementById('sessionTitleText');
    const sessionActions = document.getElementById('sessionActions');
    const deleteSessionBtn = document.getElementById('deleteSessionBtn');

    if (sessionTitleNavbar && sessionTitleText) {
        sessionTitleText.textContent = sessionData.title;
        sessionTitleNavbar.classList.remove('d-none');
        sessionTitleNavbar.classList.add('d-flex', 'align-items-center');
    }

    if (sessionActions) {
        sessionActions.classList.remove('d-none');
        sessionActions.classList.add('d-flex');
    }

    // Show delete button only for hosts
    if (deleteSessionBtn && sessionData.IsHost) {
        deleteSessionBtn.classList.remove('d-none');
    }

    // Update timer based on session settings
    setTimerDuration(sessionData.studyDuration, sessionData.breakDuration);

    // Update timer description
    const timerDesc = document.getElementById('timerDescription');
    if (timerDesc) {
        switch (sessionData.timerType) {
            case 0: // Pomodoro
                timerDesc.textContent = `Pomodoro: ${sessionData.studyDuration} min focus • ${sessionData.breakDuration} min break`;
                break;
            case 1: // Flowmodoro
                timerDesc.textContent = 'Flowmodoro: Study until you feel like taking a break';
                break;
            case 2: // Custom
                timerDesc.textContent = `Custom: ${sessionData.studyDuration} min focus • ${sessionData.breakDuration} min break`;
                break;
        }
    }

    // Update participants display
    updateParticipantsDisplay();

    // Update session stats
    document.getElementById('sessionStats').textContent =
        `${sessionData.currentParticipants} of ${sessionData.maxParticipants} participants`;
}

function updateParticipantsDisplay() {
    if (!sessionData) return;

    const participantsList = document.getElementById('participantsList');
    const participantCount = document.getElementById('participantCount');

    participantCount.textContent = sessionData.currentParticipants;

    if (sessionData.participants && sessionData.participants.length > 0) {
        participantsList.innerHTML = sessionData.participants.map(participant => `
                                    <div class="participant-item">
                                        <div class="participant-avatar-small">
                                            <i class="fas fa-user"></i>
                                        </div>
                                        <div class="participant-info">
                                            <p class="participant-name">
                                                ${participant.name}
                                                ${participant.isHost ? '<span class="host-badge ms-1">Host</span>' : ''}
                                            </p>
                                            <p class="participant-status">
                                                Joined ${new Date(participant.joinedAt).toLocaleTimeString()}
                                            </p>
                                        </div>
                                    </div>
                                `).join('');
    } else {
        participantsList.innerHTML = `
                                    <div class="text-center text-muted">
                                        <i class="fas fa-user-clock fa-2x mb-2"></i>
                                        <p>Waiting for participants...</p>
                                    </div>
                                `;
    }
}

function setupEventListeners() {
    // Allow Enter key to save todo
    document.getElementById('newTodoInput').addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            saveTodo();
        } else if (e.key === 'Escape') {
            cancelAddTodo();
        }
    });
}

// Session management functions
function copyInviteLink() {
    if (sessionData && sessionData.inviteCode) {
        const inviteText = `Join my study session "${sessionData.title}" with code: ${sessionData.inviteCode}`;
        navigator.clipboard.writeText(inviteText).then(() => {
            alert('Invite link copied to clipboard!');
        }).catch(() => {
            prompt('Copy this invite text:', inviteText);
        });
    } else if (sessionData && sessionData.isPublic) {
        const shareUrl = `${window.location.origin}/Sessions/SessionManager`;
        navigator.clipboard.writeText(`Join my study session "${sessionData.title}" at: ${shareUrl}`).then(() => {
            alert('Session link copied to clipboard!');
        }).catch(() => {
            prompt('Copy this link:', shareUrl);
        });
    } else {
        alert('This session cannot be shared.');
    }
}

function leaveSession() {
    if (confirm('Are you sure you want to leave this study session?')) {
        // End any active timer session before leaving
        if (window.currentTimerSessionId) {
            endTimerSession();
        }

        // Call API to leave session
        fetch(`/api/StudySession/LeaveSession/${sessionId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        }).then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error('Failed to leave session');
            }
        }).then(result => {
            if (result.success) {
                // Hide navbar session elements
                const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
                const sessionActions = document.getElementById('sessionActions');

                if (sessionTitleNavbar) {
                    sessionTitleNavbar.classList.add('d-none');
                }
                if (sessionActions) {
                    sessionActions.classList.add('d-none');
                }

                // Redirect to session manager
                window.location.href = '/Sessions/SessionManager';
            } else {
                alert(result.message || 'Failed to leave session');
            }
        }).catch(error => {
            console.error('Error leaving session:', error);
            alert('Error leaving session. Please try again.');
        });
    }
}

function deleteSession() {
    if (confirm('Are you sure you want to delete this study session? This action cannot be undone and will remove all participants from the session.')) {
        fetch(`/api/StudySession/DeleteSession/${sessionId}`, {
            method: 'DELETE',
            headers: {
                'Content-Type': 'application/json',
            }
        }).then(response => {
            if (response.ok) {
                return response.json();
            } else {
                throw new Error('Failed to delete session');
            }
        }).then(result => {
            if (result.success) {
                alert('Session deleted successfully');

                // Hide navbar session elements
                const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
                const sessionActions = document.getElementById('sessionActions');

                if (sessionTitleNavbar) {
                    sessionTitleNavbar.classList.add('d-none');
                }
                if (sessionActions) {
                    sessionActions.classList.add('d-none');
                }

                // Redirect to session manager
                window.location.href = '/Sessions/SessionManager';
            } else {
                alert(result.message || 'Failed to delete session');
            }
        }).catch(error => {
            console.error('Error deleting session:', error);
            alert('Error deleting session. Please try again.');
        });
    }
}

// Refresh participants periodically
setInterval(async () => {
    if (sessionId && sessionData) {
        try {
            const response = await fetch(`/api/StudySession/${sessionId}`);
            if (response.ok) {
                const updatedData = await response.json();
                if (updatedData.currentParticipants !== sessionData.currentParticipants) {
                    sessionData = updatedData;
                    updateParticipantsDisplay();
                    document.getElementById('sessionStats').textContent =
                        `${sessionData.currentParticipants} of ${sessionData.maxParticipants} participants`;
                }
            }
        } catch (error) {
            console.error('Error refreshing session data:', error);
        }
    }
}, 10000); // Refresh every 10 seconds

// Cover photo upload handler
document.getElementById('coverUpload')?.addEventListener('change', function (e) {
    if (e.target.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            // You can add logic here to display the uploaded image
            alert('Cover photo uploaded successfully!');
        };
        reader.readAsDataURL(e.target.files[0]);
    }
});

// Page leave confirmation functionality
let isLeavingSession = false;
let pendingNavigation = null;

// Handle beforeunload event (browser close, refresh, direct URL navigation)
window.addEventListener('beforeunload', function (e) {
    if (sessionId && !isLeavingSession) {
        e.preventDefault();
        e.returnValue = ''; // This triggers the browser's default confirmation dialog
        return '';
    }
});

// Handle internal navigation (clicking links, form submissions, etc.)
document.addEventListener('click', function (e) {
    if (sessionId && !isLeavingSession) {
        // Check if it's a navigation link (excluding session-specific actions)
        const link = e.target.closest('a');
        if (link && link.href) {
            const currentUrl = window.location.href;
            const linkUrl = link.href;

            // Allow navigation within the same page or to specific actions
            if (linkUrl.includes('#') ||
                linkUrl === currentUrl ||
                link.classList.contains('allow-navigation') ||
                link.hasAttribute('data-allow-navigation')) {
                return;
            }

            // Prevent navigation and show custom confirmation
            e.preventDefault();
            showLeaveSessionModal(linkUrl);
        }
    }
});

// Custom confirmation modal functions
function showLeaveSessionModal(targetUrl = null) {
    pendingNavigation = targetUrl;
    document.getElementById('leaveSessionModal').style.display = 'block';
    document.body.style.overflow = 'hidden'; // Prevent background scrolling
}

function hideLeaveSessionModal() {
    document.getElementById('leaveSessionModal').style.display = 'none';
    document.body.style.overflow = 'auto'; // Restore scrolling
    pendingNavigation = null;
}

function confirmLeaveSession() {
    // Show loading spinner
    document.getElementById('leavingSpinner').style.display = 'block';
    document.getElementById('modalButtons').style.display = 'none';

    isLeavingSession = true;

    // End any active timer session before leaving
    if (window.currentTimerSessionId) {
        endTimerSession();
    }

    // Call the leave session API
    fetch(`/api/StudySession/LeaveSession/${sessionId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    }).then(response => {
        if (response.ok) {
            return response.json();
        } else {
            throw new Error('Failed to leave session');
        }
    }).then(result => {
        if (result.success) {
            // Hide navbar session elements
            const sessionTitleNavbar = document.getElementById('sessionTitleNavbar');
            const sessionActions = document.getElementById('sessionActions');

            if (sessionTitleNavbar) {
                sessionTitleNavbar.classList.add('d-none');
            }
            if (sessionActions) {
                sessionActions.classList.add('d-none');
            }

            // Navigate to target URL or default
            setTimeout(() => {
                if (pendingNavigation) {
                    window.location.href = pendingNavigation;
                } else {
                    window.location.href = '/Sessions/SessionManager';
                }
            }, 500);
        } else {
            isLeavingSession = false; // Reset flag on failure
            hideLeaveSessionModal();
            alert(result.message || 'Failed to leave session');
        }
    }).catch(error => {
        isLeavingSession = false; // Reset flag on error
        hideLeaveSessionModal();
        console.error('Error leaving session:', error);
        alert('Error leaving session. Please try again.');
    });
}

// Modal event listeners
document.getElementById('btnStayInSession').addEventListener('click', hideLeaveSessionModal);
document.getElementById('btnLeaveSession').addEventListener('click', confirmLeaveSession);

// Close modal when clicking outside
document.getElementById('leaveSessionModal').addEventListener('click', function (e) {
    if (e.target === this) {
        hideLeaveSessionModal();
    }
});

// Update the existing leaveSession function to use the new modal
window.originalLeaveSession = leaveSession;
leaveSession = function () {
    showLeaveSessionModal();
};

// Handle browser navigation (back/forward buttons)
window.addEventListener('popstate', function (e) {
    if (sessionId && !isLeavingSession) {
        // Prevent the navigation
        history.pushState(null, null, window.location.href);
        showLeaveSessionModal();
    }
});

// Push current state to enable popstate detection
if (sessionId) {
    history.pushState(null, null, window.location.href);
}