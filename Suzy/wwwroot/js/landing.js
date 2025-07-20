// Script for Suzy Landing Page
document.addEventListener('DOMContentLoaded', function() {
    
    // Form handling
    const signupForm = document.getElementById('signupForm');
    const signinBtn = document.getElementById('signinBtn');
    const startStudyingBtn = document.getElementById('startStudyingBtn');
    const learnMoreBtn = document.getElementById('learnMoreBtn');
    const mobileMenuBtn = document.getElementById('mobileMenuBtn');

    // Signup form submission
    if (signupForm) {
        signupForm.addEventListener('submit', function(e) {
            e.preventDefault();
            
            const email = document.getElementById('email').value;
            const password = document.getElementById('password').value;
            
            if (email && password) {
                // Redirect to ASP.NET Core Identity registration
                window.location.href = '/Identity/Account/Register';
            }
        });
    }

    // Sign in button
    if (signinBtn) {
        signinBtn.addEventListener('click', function() {
            // Redirect to ASP.NET Core Identity login
            window.location.href = '/Identity/Account/Login';
        });
    }

    // Login button in nav
    const loginBtn = document.querySelector('.btn-outline');
    if (loginBtn) {
        loginBtn.addEventListener('click', function() {
            window.location.href = '/Identity/Account/Login';
        });
    }

    // Start studying button
    if (startStudyingBtn) {
        startStudyingBtn.addEventListener('click', function() {
            // Scroll to signup form
            const signupCard = document.querySelector('.signup-card');
            if (signupCard) {
                signupCard.scrollIntoView({ behavior: 'smooth' });
            }
        });
    }

    // Learn more button
    if (learnMoreBtn) {
        learnMoreBtn.addEventListener('click', function() {
            // Scroll to features section
            const featuresSection = document.getElementById('features');
            if (featuresSection) {
                featuresSection.scrollIntoView({ behavior: 'smooth' });
            }
        });
    }

    // Mobile menu toggle (basic implementation)
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function() {
            const navLinks = document.querySelector('.nav-links');
            if (navLinks) {
                navLinks.style.display = navLinks.style.display === 'flex' ? 'none' : 'flex';
            }
        });
    }

    // Smooth scrolling for navigation links
    const navLinks = document.querySelectorAll('a[href^="#"]');
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            
            const targetId = this.getAttribute('href');
            const targetSection = document.querySelector(targetId);
            
            if (targetSection) {
                targetSection.scrollIntoView({ 
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Add scroll effect to feature cards
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function(entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.animation = 'fadeIn 0.6s ease-out';
            }
        });
    }, observerOptions);

    // Observe feature cards and other sections
    const featureCards = document.querySelectorAll('.feature-card');
    const sections = document.querySelectorAll('.stats, .about, .cta');
    
    featureCards.forEach(card => observer.observe(card));
    sections.forEach(section => observer.observe(section));

    // Add hover effects to buttons
    const buttons = document.querySelectorAll('.btn');
    buttons.forEach(button => {
        button.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-2px)';
        });
        
        button.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
        });
    });

    // Initialize any additional animations or effects
    console.log('Suzy Landing Page loaded successfully!');
});
