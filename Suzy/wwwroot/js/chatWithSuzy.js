
let currentConversationId = null;
let currentPath = null;

$(document).ready(function () {
    console.log('Chat page loaded');
    console.log('jQuery loaded:', typeof $ !== 'undefined');
    
    loadAnalytics();

    $('#sendMessageBtn').click(function() {
        sendMessage();
    });
    $('#messageInput').keypress(function (e) {
        if (e.which === 13) {
            sendMessage();
        }
    });

    // Handle topic button clicks - single clean handler
    $(document).on('click', '.topic-button', function (e) {
        e.preventDefault();
        console.log('Topic button clicked');
        const pathType = $(this).attr('data-path-type');
        if (pathType) {
            startConversation(pathType);
        }
    });

    // Handle conversation option clicks
    $(document).on('click', '.conversation-option', function (e) {
        e.preventDefault();
        console.log('Conversation option clicked');
        const message = $(this).text().trim();
        handleUserResponse(message);
    });

    // Handle restart button clicks
    $(document).on('click', '.restart-button', function (e) {
        e.preventDefault();
        console.log('Restart button clicked');
        restartChat();
    });
});

function startConversation(pathType) {
    console.log('Starting conversation for path:', pathType);
    currentPath = pathType;
    
    // Clear messages and show input
    $('#messagesContainer').empty();
    $('#messageInputContainer').show();

    // Add user's initial message based on topic
    const topicMessages = {
        'study-time': 'Hi Suzy! I would love to get a comprehensive analysis of my study time patterns. Could you please examine my recent study sessions, break down my daily and weekly study habits, identify any trends or patterns you notice, and provide detailed insights on how I can optimize my study schedule? I\'m particularly interested in understanding my peak productivity hours, consistency patterns, and areas where I might be able to improve my time management.',
        'focus': 'Hello Suzy! I\'m concerned about my focus and concentration levels during study sessions. Could you please provide a detailed analysis of my focus patterns this week? I\'d like to understand how well I\'ve been maintaining concentration, what factors might be affecting my focus, how my break-to-study ratios look, and get comprehensive recommendations on improving my concentration and avoiding distractions during study sessions.',
        'todo': 'Hello Suzy! I want to evaluate my productivity and task management. Could you please analyze my task completion patterns, examine which types of tasks I complete most efficiently, identify any productivity bottlenecks or recurring challenges, and provide comprehensive strategies for improving my task management and overall productivity? I\'d also like suggestions for better prioritization and time allocation.',
        'weekly': 'Hi Suzy! I\'d like a comprehensive weekly summary of my study progress and performance. Could you please analyze all aspects of my study activities from this past week, including time spent studying, topics covered, productivity patterns, achievements and challenges, and provide detailed insights on my overall progress? I\'m looking for both celebrations of what went well and actionable recommendations for improvement.',
        'mock-exam': 'Hello Suzy! I recently took a mock exam and would appreciate a detailed performance analysis. Could you examine my results, identify my strongest and weakest subject areas, analyze my time management during the exam, pinpoint specific topics that need more attention, and provide a comprehensive study plan to address my weak areas? I\'m particularly interested in strategic recommendations for improving my exam performance and confidence.'
    };
    
    const initialMessage = topicMessages[pathType];
    console.log('Initial message:', initialMessage);
    addMessage(initialMessage, true);

    // Start conversation with API
    startConversationAPI(pathType, initialMessage);
}

function startConversationAPI(pathType, message) {
    // Ensure any existing typing indicator is removed first
    removeTypingIndicator();
    addTypingIndicator();

    // Map frontend path types to backend enum values
    const pathTypeMapping = {
        'study-time': 1, // StudyTimeAnalysis
        'focus': 2,      // FocusAndPauses
        'todo': 4,       // TodoProductivity
        'weekly': 5,     // WeeklySummary
        'mock-exam': 6   // MockExamReview
    };

    // First start the conversation
    $.ajax({
        url: '/api/ChatAnalytics/conversation/start',
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            PathType: pathTypeMapping[pathType]
        })
    })
    .done(function (response) {
        currentConversationId = response.id;
        // Now send the initial message
        sendMessageToAPI(message);
    })
    .fail(function (xhr, status, error) {
        removeTypingIndicator();
        console.error('API Error starting conversation:', error);
        addMessage("I'm sorry, I'm having trouble starting our conversation right now. Please try again in a moment.", false);
        addRestartButton();
    });
}

function sendToAPI(message) {
    if (!currentConversationId) {
        console.error('No conversation ID available');
        return;
    }
    sendMessageToAPI(message);
}

function sendMessageToAPI(message) {
    if (!currentConversationId) {
        console.error('No conversation ID available');
        return;
    }

    // Ensure any existing typing indicator is removed first
    removeTypingIndicator();
    addTypingIndicator();

    // Send message to existing conversation
    $.ajax({
        url: `/api/ChatAnalytics/conversation/${currentConversationId}/message`,
        type: 'POST',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val(),
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({
            Message: message
        })
    })
    .done(function (response) {
        removeTypingIndicator();
        addMessage(response.response, false);
        
        // For now, always add restart button since we don't have suggestion system yet
        addRestartButton();
    })
    .fail(function (xhr, status, error) {
        removeTypingIndicator();
        console.error('API Error sending message:', error);
        addMessage("I'm sorry, I'm having trouble processing your message right now. Please try again in a moment.", false);
        addRestartButton();
    });
}

function handleUserResponse(message) {
    addMessage(message, true);
    sendToAPI(message);
}

function sendMessage() {
    const messageInput = $('#messageInput');
    const message = messageInput.val().trim();
    
    if (message) {
        addMessage(message, true);
        messageInput.val('');
        
        // Send to API
        sendToAPI(message);
    }
}

function addConversationOptions(options) {
    const optionsHtml = options.map(option => 
        `<button class="conversation-option">${option}</button>`
    ).join('');
    
    const optionsContainer = $(`
        <div class="message-wrapper bot-message-container">
            <div class="suzy-profile">
                <img src="/lib/assets/SuzyChatIcon.png" alt="Suzy" class="suzy-avatar-small">
            </div>
            <div class="conversation-options">
                ${optionsHtml}
            </div>
        </div>
    `);
    
    $('#messagesContainer').append(optionsContainer);
    scrollToBottom();
}

function addRestartButton() {
    const restartHtml = $(`
        <div class="message-wrapper bot-message-container">
            <div class="suzy-profile">
                <img src="/lib/assets/SuzyChatIcon.png" alt="Suzy" class="suzy-avatar-small">
            </div>
            <div class="conversation-options">
                <button class="restart-button">🔄 Start New Conversation</button>
            </div>
        </div>
    `);
    
    $('#messagesContainer').append(restartHtml);
    scrollToBottom();
}

function restartChat() {
    $('#messagesContainer').empty();
    $('#messageInputContainer').hide();
    
    // Show the welcome message with topics again
    const welcomeHtml = $(`
        <div class="welcome-message" id="welcomeMessage">
            <div class="suzy-profile">
                <img src="/lib/assets/SuzyChatIcon.png" alt="Suzy" class="suzy-avatar">
            </div>
            <h5 class="welcome-title">Hi! I'm Suzy, your AI study assistant!</h5>
            <p class="welcome-text">What can I help you with today?</p>
            
            <div class="conversation-topics" id="conversationTopics">
                <div class="topic-button" data-path-type="study-time">
                    <div class="topic-icon">📊</div>
                    <div class="topic-text">Analyze my study time</div>
                </div>
                <div class="topic-button" data-path-type="focus">
                    <div class="topic-icon">🎯</div>
                    <div class="topic-text">Was I focused this week?</div>
                </div>
                <div class="topic-button" data-path-type="todo">
                    <div class="topic-icon">✅</div>
                    <div class="topic-text">Did I complete my tasks?</div>
                </div>
                <div class="topic-button" data-path-type="weekly">
                    <div class="topic-icon">📈</div>
                    <div class="topic-text">Give me a weekly summary</div>
                </div>
                <div class="topic-button" data-path-type="mock-exam">
                    <div class="topic-icon">📝</div>
                    <div class="topic-text">How did I do in my last mock exam?</div>
                </div>
            </div>
        </div>
    `);
    
    $('#messagesContainer').append(welcomeHtml);
    
    currentPath = null;
    currentConversationId = null;
}

function loadAnalytics() {
    // Load today's analytics
    $.ajax({
        url: '/api/ChatAnalytics/analytics/today',
        type: 'GET',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        }
    })
        .done(function (data) {
            $('#todayStudyTime').text(formatMinutes(data.totalStudyMinutes));
            $('#todayBreakTime').text(formatMinutes(data.totalBreakMinutes));
        })
        .fail(function () {
            $('#todayStudyTime').text('0m');
            $('#todayBreakTime').text('0m');
        });

    // Load average daily study time for past 7 days
    $.ajax({
        url: '/api/ChatAnalytics/analytics/weekly',
        type: 'GET',
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        }
    })
        .done(function (data) {
            $('#weeklyAverage').text(formatMinutes(Math.round(data.averageStudyTimePerDay)));
        })
        .fail(function () {
            $('#weeklyAverage').text('0m');
        });
}

function addMessage(content, isUser) {
const messageClass = isUser ? 'user-message' : 'bot-message';
const alignment = isUser ? 'user-message-container' : 'bot-message-container';
const username = isUser ? 'You' : 'Suzy';

// Convert bot's markdown response to HTML before displaying
let formattedContent;
if (isUser) {
// User messages are plain text
formattedContent = content;
} else {
// Bot messages are parsed from Markdown to HTML
formattedContent = marked.parse(content);
}

let messageHtml;
if (isUser) {
messageHtml = $(`
<div class="message-wrapper ${alignment}">
    <div class="message-bubble ${messageClass}">
        <div class="message-header">${username}</div>
        <div class="message-content">${formattedContent}</div>
    </div>
</div>
`);
} else {
messageHtml = $(`
<div class="message-wrapper ${alignment}">
    <div class="suzy-profile">
        <img src="/lib/assets/SuzyChatIcon.png" alt="Suzy" class="suzy-avatar-small">
    </div>
    <div class="message-bubble ${messageClass}">
        <div class="message-header">${username}</div>
        <div class="message-content">${formattedContent}</div>
    </div>
</div>
`);
}

$('#messagesContainer').append(messageHtml);
scrollToBottom();
}

function addTypingIndicator() {
    const typingHtml = $(`
        <div class="message-wrapper bot-message-container" id="typingIndicator">
            <div class="suzy-profile">
                <img src="/lib/assets/SuzyChatIcon.png" alt="Suzy" class="suzy-avatar-small">
            </div>
            <div class="message-bubble bot-message">
                <div class="message-header">Suzy</div>
                <div class="message-content">
                    <div class="typing-dots">
                        <span>.</span><span>.</span><span>.</span>
                    </div>
                </div>
            </div>
        </div>
    `);

    $('#messagesContainer').append(typingHtml);
    scrollToBottom();
}

function removeTypingIndicator() {
    // Remove all typing indicators (in case there are multiple)
    $('#typingIndicator, .typing-indicator').remove();
}

function scrollToBottom() {
    const container = $('#messagesContainer');
    container.scrollTop(container[0].scrollHeight);
}

function formatMinutes(minutes) {
    if (minutes < 60) {
        return minutes + 'm';
    }
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return hours + 'h' + (remainingMinutes > 0 ? ' ' + remainingMinutes + 'm' : '');
}