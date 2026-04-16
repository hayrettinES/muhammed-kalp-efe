// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('DOMContentLoaded', () => {
    const cursor = document.getElementById('global-cursor');
    if(cursor) {
        document.addEventListener('mousemove', e => {
            cursor.style.left = e.clientX + 'px';
            cursor.style.top  = e.clientY + 'px';
        });
        
        // Etkileşimli elementlerin üzerine gelindiğinde cursor büyüsün
        const interactibles = document.querySelectorAll('a, button, input, textarea, select, .buton');
        interactibles.forEach(el => {
            el.addEventListener('mouseenter', () => cursor.classList.add('grow'));
            el.addEventListener('mouseleave', () => cursor.classList.remove('grow'));
        });
    }
});
